using ConvenienceStore.Web.Tests.HaTang;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

namespace ConvenienceStore.Web.Tests.Integration.Database.Schema;

[CollectionDefinition(Name)]
public sealed class SchemaCollection : ICollectionFixture<CsdlMySqlTam>
{
    public const string Name = "UnifiedSchema";
}

// Adapted from PR #168. Fixture creates a fresh DB through the runtime migration;
// no hard-coded shared database, opt-in skip, or second context/migration lineage.
[Collection(SchemaCollection.Name)]
[Trait("Category", "DatabaseIntegration")]
public sealed class SchemaStructureTests(CsdlMySqlTam database)
{
    private async Task<string[]> ReadAsync(string sql)
    {
        await using var connection = await database.MoKetNoiAsync();
        await using var command = new MySqlCommand(sql, connection) { CommandTimeout = 30 };
        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<string>();
        while (await reader.ReadAsync()) rows.Add(reader.GetString(0));
        return rows.Order().ToArray();
    }

    [Fact]
    public async Task DB01_Has28BusinessTablesPlusEfHistory()
    {
        var tables = await ReadAsync("""
            SELECT TABLE_NAME FROM information_schema.TABLES
            WHERE TABLE_SCHEMA=DATABASE() AND TABLE_TYPE='BASE TABLE'
            """);
        Assert.Equal(29, tables.Length);
        Assert.Contains("__EFMigrationsHistory", tables);
    }

    [Fact]
    public async Task DB02_TableIdentitiesMatchReferenceAndRuntimeModel()
    {
        var tables = await ReadAsync("""
            SELECT TABLE_NAME FROM information_schema.TABLES
            WHERE TABLE_SCHEMA=DATABASE() AND TABLE_TYPE='BASE TABLE'
              AND TABLE_NAME <> '__EFMigrationsHistory'
            """);
        Assert.Equal(SchemaReference.Tables, tables);
        await using var db = database.TaoDbContext();
        Assert.Equal(tables, db.Model.GetRelationalModel().Tables.Select(t => t.Name).Order());
    }

    [Fact]
    public async Task DB03_ViewIdentitiesMatchReferenceAndRuntimeModel()
    {
        var views = await ReadAsync("""
            SELECT TABLE_NAME FROM information_schema.VIEWS WHERE TABLE_SCHEMA=DATABASE()
            """);
        Assert.Equal(SchemaReference.Views, views);
        await using var db = database.TaoDbContext();
        Assert.Equal(views, db.Model.GetRelationalModel().Views.Select(v => v.Name).Order());
    }

    [Theory]
    [InlineData("v_chuong_trinh_khuyen_mai")]
    [InlineData("v_gia_ban_hien_hanh")]
    [InlineData("v_luot_dung_khuyen_mai")]
    public async Task DB04_ViewCanBeQueriedWithoutReadingRows(string view)
    {
        Assert.Contains(view, SchemaReference.Views);
        await using var connection = await database.MoKetNoiAsync();
        await using var command = new MySqlCommand($"SELECT * FROM `{view}` LIMIT 0", connection);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(reader.FieldCount > 0);
    }

    [Fact]
    public async Task DB05_AllTenTriggerNamesTargetsTimingsAndEventsMatchReference()
    {
        var triggers = await ReadAsync("""
            SELECT CONCAT(TRIGGER_NAME,'|',ACTION_TIMING,'|',EVENT_MANIPULATION,'|',EVENT_OBJECT_TABLE)
            FROM information_schema.TRIGGERS WHERE TRIGGER_SCHEMA=DATABASE()
            """);
        Assert.Equal(10, triggers.Length);
        Assert.Equal(SchemaReference.Triggers, triggers);
    }

    [Fact]
    public async Task DB06_All53NamedChecksExistAndAreEnforced()
    {
        var checks = await ReadAsync("""
            SELECT CONSTRAINT_NAME FROM information_schema.TABLE_CONSTRAINTS
            WHERE TABLE_SCHEMA=DATABASE() AND CONSTRAINT_TYPE='CHECK' AND ENFORCED='YES'
            """);
        Assert.Equal(53, checks.Length);
        Assert.Equal(SchemaReference.Checks, checks);
    }

    [Fact]
    public async Task DB07_AppliedMigrationsMatchRuntimeLineage()
    {
        await using var db = database.TaoDbContext();
        var applied = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
        Assert.Equal(new[] { "20261026120030_InitialCreate" }, applied);
        Assert.Equal(db.Database.GetMigrations(), applied);
    }

    [Fact]
    public async Task DB08_NoPendingMigrations_AndReapplyingIsIdempotent()
    {
        await using var db = database.TaoDbContext();
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        await db.Database.MigrateAsync();
        Assert.Single(await db.Database.GetAppliedMigrationsAsync());
    }

    [Fact]
    public async Task DB09_AllPrimaryKeysMatchRuntimeModel()
    {
        var keys = await ReadAsync("""
            SELECT CONCAT(TABLE_NAME,'|',GROUP_CONCAT(COLUMN_NAME ORDER BY ORDINAL_POSITION SEPARATOR ','))
            FROM information_schema.KEY_COLUMN_USAGE WHERE TABLE_SCHEMA=DATABASE()
              AND CONSTRAINT_NAME='PRIMARY' AND TABLE_NAME <> '__EFMigrationsHistory'
            GROUP BY TABLE_NAME
            """);
        await using var db = database.TaoDbContext();
        Assert.Equal(db.Model.GetRelationalModel().Tables.Select(t =>
            $"{t.Name}|{string.Join(',', t.PrimaryKey!.Columns.Select(c => c.Name))}").Order(), keys);
    }

    [Fact]
    public async Task DB10_AllMappedColumnNamesMatchDatabase()
    {
        var columns = await ReadAsync("""
            SELECT CONCAT(TABLE_NAME,'|',COLUMN_NAME) FROM information_schema.COLUMNS
            WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME <> '__EFMigrationsHistory'
            """);
        await using var db = database.TaoDbContext();
        var model = db.Model.GetRelationalModel();
        Assert.Equal(model.Tables.SelectMany(t => t.Columns.Select(c => $"{t.Name}|{c.Name}"))
            .Concat(model.Views.SelectMany(v => v.Columns.Select(c => $"{v.Name}|{c.Name}"))).Order(), columns);
    }

    [Fact]
    public async Task DB11_ForeignKeysAndDeleteRulesMatchRuntimeModel()
    {
        var keys = await ReadAsync("""
            SELECT CONCAT(k.TABLE_NAME,'|',k.CONSTRAINT_NAME,'|',
              GROUP_CONCAT(k.COLUMN_NAME ORDER BY k.ORDINAL_POSITION SEPARATOR ','),'|',
              k.REFERENCED_TABLE_NAME,'|',
              GROUP_CONCAT(k.REFERENCED_COLUMN_NAME ORDER BY k.ORDINAL_POSITION SEPARATOR ','),'|',r.DELETE_RULE)
            FROM information_schema.KEY_COLUMN_USAGE k JOIN information_schema.REFERENTIAL_CONSTRAINTS r
              ON r.CONSTRAINT_SCHEMA=k.CONSTRAINT_SCHEMA AND r.CONSTRAINT_NAME=k.CONSTRAINT_NAME
            WHERE k.TABLE_SCHEMA=DATABASE() AND k.REFERENCED_TABLE_NAME IS NOT NULL
            GROUP BY k.TABLE_NAME,k.CONSTRAINT_NAME,k.REFERENCED_TABLE_NAME,r.DELETE_RULE
            """);
        await using var db = database.TaoDbContext();
        Assert.Equal(db.Model.GetRelationalModel().Tables.SelectMany(t => t.ForeignKeyConstraints.Select(f =>
            $"{t.Name}|{f.Name}|{string.Join(',', f.Columns.Select(c => c.Name))}|{f.PrincipalTable.Name}|" +
            $"{string.Join(',', f.PrincipalColumns.Select(c => c.Name))}|{(f.OnDeleteAction == Microsoft.EntityFrameworkCore.Migrations.ReferentialAction.Cascade ? "CASCADE" : "NO ACTION")}"))
            .Order(), keys.Select(k => k.Replace("|RESTRICT", "|NO ACTION")).Order());
    }

    [Fact]
    public async Task DB12_UniqueKeysAndIndexesMatchRuntimeModel()
    {
        var indexes = await ReadAsync("""
            SELECT CONCAT(TABLE_NAME,'|',INDEX_NAME,'|',NON_UNIQUE,'|',
              GROUP_CONCAT(COLUMN_NAME ORDER BY SEQ_IN_INDEX SEPARATOR ','))
            FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE()
              AND TABLE_NAME <> '__EFMigrationsHistory' AND INDEX_NAME <> 'PRIMARY'
            GROUP BY TABLE_NAME,INDEX_NAME,NON_UNIQUE
            """);
        await using var db = database.TaoDbContext();
        var expected = db.Model.GetRelationalModel().Tables.SelectMany(t =>
            t.Indexes.Select(i => $"{t.Name}|{i.Name}|{(i.IsUnique ? 0 : 1)}|{string.Join(',', i.Columns.Select(c => c.Name))}")).Order();
        Assert.Equal(expected, indexes);
        // EF's synthetic alternate-key name need not be the physical index name.
        // The referenced columns must still be backed by a unique physical key.
        foreach (var table in db.Model.GetRelationalModel().Tables)
        foreach (var key in table.UniqueConstraints.Where(k => k != table.PrimaryKey))
            Assert.Contains(indexes, i => i.StartsWith(table.Name + "|", StringComparison.Ordinal) &&
                i.EndsWith($"|0|{string.Join(',', key.Columns.Select(c => c.Name))}", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DB13_ColumnTypesPrecisionAndNullabilityMatchRuntimeModel()
    {
        var columns = await ReadAsync("""
            SELECT CONCAT(c.TABLE_NAME,'|',c.COLUMN_NAME,'|',LOWER(c.COLUMN_TYPE),'|',c.IS_NULLABLE)
            FROM information_schema.COLUMNS c JOIN information_schema.TABLES t
              ON t.TABLE_SCHEMA=c.TABLE_SCHEMA AND t.TABLE_NAME=c.TABLE_NAME
            WHERE c.TABLE_SCHEMA=DATABASE() AND t.TABLE_TYPE='BASE TABLE'
              AND c.TABLE_NAME <> '__EFMigrationsHistory'
            """);
        await using var db = database.TaoDbContext();
        var expected = db.Model.GetRelationalModel().Tables.SelectMany(t => t.Columns.Select(c =>
            $"{t.Name}|{c.Name}|{c.StoreType.ToLowerInvariant()}|{(c.IsNullable ? "YES" : "NO")}")).Order();
        Assert.Equal(expected, columns);
    }
}
