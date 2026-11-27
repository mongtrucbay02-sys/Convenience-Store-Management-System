using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Text.Json;

namespace ConvenienceStore.Web.Tests.PostgreSql;

public sealed class SupabaseModelMappingTests
{
    private static CuaHangDbContext Create() => new(new DbContextOptionsBuilder<CuaHangDbContext>()
        .UseNpgsql("Host=127.0.0.1;Database=unused;Username=unused", options => options.SetPostgresVersion(17, 0)).Options);

    [Fact]
    public void Model_MatchesReadOnlySupabaseCatalog()
    {
        using var db = Create();
        using var contract = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,
            "PostgreSql", "supabase-schema-contract.json")));
    var model=db.GetService<IDesignTimeModel>().Model;
    var relational=model.GetRelationalModel();
    var tables=relational.Tables.Where(t=>t.Schema=="smartmart").ToDictionary(t=>t.Name);
    var errors=new List<string>();
    var expected=contract.RootElement.GetProperty("tables");
    if(tables.Count!=expected.GetArrayLength())errors.Add($"table count {tables.Count}");
    int columns=0;
    foreach(var table in expected.EnumerateArray()) {
        var name=table.GetProperty("name").GetString()!;
        if(!tables.TryGetValue(name,out var mapped)){errors.Add("Missing table "+name);continue;}
        var expectedColumns=table.GetProperty("columns");
        // Keep the captured Supabase baseline unchanged; verify the explicit migration delta separately.
        if(mapped.Columns.Count()!=expectedColumns.GetArrayLength()+(name=="phieu_doi_tra"?1:0))errors.Add("Column count "+name);
        foreach(var column in expectedColumns.EnumerateArray()) {
            columns++;
            var columnName=column.GetProperty("name").GetString()!;
            var actual=mapped.Columns.SingleOrDefault(c=>c.Name==columnName);
            if(actual is null){errors.Add($"Missing {name}.{columnName}");continue;}
            var udt=column.GetProperty("udt").GetString();
            var type=udt switch {
                "int8"=>"bigint","int4"=>"integer","int2"=>"smallint","bool"=>"boolean",
                "date"=>"date","timestamptz"=>"timestamp with time zone",
                "varchar"=>$"character varying({column.GetProperty("length").GetInt32()})",
                "bpchar"=>$"character({column.GetProperty("length").GetInt32()})",
                "numeric"=>column.GetProperty("precision").ValueKind==JsonValueKind.Null?"numeric":$"numeric({column.GetProperty("precision").GetInt32()},{column.GetProperty("scale").GetInt32()})",
                _=>column.GetProperty("type").GetString()
            };
            if(actual.StoreType!=type)errors.Add($"Type {name}.{columnName}: {actual.StoreType} != {type}");
            if(actual.IsNullable!=(column.GetProperty("nullable").GetString()=="YES"))errors.Add($"Nullability {name}.{columnName}");
            if(column.GetProperty("default").ValueKind!=JsonValueKind.Null &&
                actual.PropertyMappings.All(p=>p.Property.FindAnnotation(RelationalAnnotationNames.DefaultValue) is null
                    && p.Property.GetDefaultValueSql() is null)) errors.Add($"Default {name}.{columnName}");
            if(column.GetProperty("generated").GetString()=="ALWAYS" && actual.PropertyMappings.Any(p=>p.Property.ValueGenerated!=ValueGenerated.OnAddOrUpdate))errors.Add($"Generated {name}.{columnName}");
        }
    }
    var constraints=contract.RootElement.GetProperty("constraints").EnumerateArray().ToArray();
    var fks=tables.Values.SelectMany(t=>t.ForeignKeyConstraints).ToArray();
    foreach(var fk in constraints.Where(c=>c.GetProperty("kind").GetString()=="f")) {
        var name=fk.GetProperty("name").GetString();
        var actual = fks.SingleOrDefault(f => f.Name == name);
        if(actual is null) { errors.Add("Missing FK "+name); continue; }
        Assert.Equal(fk.GetProperty("table").GetString(), actual.Table.Schema + "." + actual.Table.Name);
        var definition = System.Text.RegularExpressions.Regex.Match(fk.GetProperty("definition").GetString()!,
            @"FOREIGN KEY \(([^)]+)\) REFERENCES smartmart\.(\w+)\(([^)]+)\)");
        Assert.True(definition.Success);
        Assert.Equal(definition.Groups[1].Value.Split(", "), actual.Columns.Select(c => c.Name));
        Assert.Equal(definition.Groups[2].Value, actual.PrincipalTable.Name);
        Assert.Equal(definition.Groups[3].Value.Split(", "), actual.PrincipalColumns.Select(c => c.Name));
    }
    var checkCount=tables.Values.Sum(t=>t.CheckConstraints.Count());
    if(checkCount!=constraints.Count(c=>c.GetProperty("kind").GetString()=="c"))errors.Add("Check count "+checkCount);
    var triggerCount=model.GetEntityTypes().Sum(e=>e.GetDeclaredTriggers().Count());
    var views=relational.Views.Where(v=>v.Schema=="smartmart").Select(v=>v.Name).ToArray();
    foreach(var view in contract.RootElement.GetProperty("views").EnumerateArray())
        if(!views.Contains(view.GetProperty("name").GetString()))errors.Add("Missing view "+view.GetProperty("name").GetString());
    Assert.Equal(31, tables.Count); Assert.Equal(307, columns); Assert.Equal(62, fks.Length); Assert.Equal(89, checkCount); Assert.Equal(17, triggerCount); Assert.Equal(3, views.Length);
    if(errors.Count!=0)throw new Exception(string.Join(Environment.NewLine,errors));

        Assert.Equal(31, tables.Values.Count(t => t.PrimaryKey is not null));
        foreach (var check in constraints.Where(c => c.GetProperty("kind").GetString() == "c"))
        {
            var actual = Assert.Single(tables.Values.SelectMany(t => t.CheckConstraints),
                c => c.Name == check.GetProperty("name").GetString());
            Assert.Equal(check.GetProperty("definition").GetString()![7..^1], actual.Sql);
        }
        foreach (var trigger in contract.RootElement.GetProperty("triggers").EnumerateArray())
            Assert.Contains(model.GetEntityTypes().SelectMany(e => e.GetDeclaredTriggers()),
                t => t.ModelName == trigger.GetProperty("name").GetString()
                    && t.EntityType.GetTableName() == trigger.GetProperty("table").GetString());
        Assert.All(model.GetEntityTypes(), e => Assert.Equal("smartmart", e.GetSchema() ?? e.GetViewSchema()));
        Assert.All(model.GetEntityTypes().Where(e => e.GetViewName() is not null), e => Assert.Null(e.FindPrimaryKey()));
    }

    [Fact]
    public void NativeTypes_UseSignedBigintDateOnlyAndSessionExpiry()
    {
        using var db = Create();
        var exchangePlan = db.Model.FindEntityType(typeof(PhieuDoiTra))!.FindProperty("PhuongAnDoi")!;
        Assert.True(exchangePlan.IsNullable);
        Assert.Equal("jsonb", exchangePlan.GetColumnType());
        Assert.Equal(typeof(long), db.Model.FindEntityType(typeof(SanPham))!.FindProperty("Id")!.ClrType);
        Assert.Equal(typeof(short), db.Model.FindEntityType(typeof(VaiTro))!.FindProperty("Id")!.ClrType);
        Assert.Equal(typeof(DateOnly), db.Model.FindEntityType(typeof(PhieuNhap))!.FindProperty("NgayNhap")!.ClrType);
        Assert.Equal("timestamp with time zone", db.Model.FindEntityType(typeof(PhienDangNhap))!.FindProperty("HetHanLuc")!.GetColumnType());
        Assert.True(db.Model.FindEntityType(typeof(DanhMuc))!.FindProperty("PhienBan")!.IsConcurrencyToken);
        Assert.NotNull(db.Model.FindEntityType(typeof(GiaoDichTienMat))!.FindProperty("QuyTienMatId"));
    }

    [Fact]
    public void GeneratedColumns_AreReadBackAndNeverSentInInsertOrUpdate()
    {
        using var db = Create();
        var generated = db.Model.GetEntityTypes().SelectMany(e => e.GetProperties())
            .Where(p => p.GetComputedColumnSql() is not null).ToArray();
        Assert.Equal(5, generated.Length);
        Assert.All(generated, p => {
            Assert.Equal(ValueGenerated.OnAddOrUpdate, p.ValueGenerated);
            Assert.Equal(PropertySaveBehavior.Ignore, p.GetBeforeSaveBehavior());
            Assert.Equal(PropertySaveBehavior.Ignore, p.GetAfterSaveBehavior());
        });
    }

    [Fact]
    public void Baseline_IsPostgreSqlOnlyAndMatchesCurrentSnapshot()
    {
        using var db=Create();
        Assert.Equal(new[] { "20261124050401_PostgreSqlBaseline", "20261127030000_ExchangePlanSnapshots" },
            db.Database.GetMigrations());
        Assert.False(db.Database.HasPendingModelChanges());
        var script=db.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>().GenerateScript();
        Assert.Contains("CREATE VIEW smartmart.v_gia_ban_hien_hanh",script);
        Assert.Contains("ADD phuong_an_doi jsonb", script);
        Assert.DoesNotContain("ENGINE=",script);
    }
}
