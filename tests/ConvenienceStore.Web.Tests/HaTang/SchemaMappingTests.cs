using ConvenienceStore.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ConvenienceStore.Web.Tests.HaTang;

[Trait("Category", "Mapping")]
public sealed class SchemaMappingTests
{
    private static CuaHangDbContext Context() => new(
        new DbContextOptionsBuilder<CuaHangDbContext>()
            .UseMySQL("server=localhost;database=metadata_only").Options);

    [Fact]
    public void RuntimeHasOneContext_AndMapsEveryReferenceTableAndViewOnce()
    {
        Assert.Equal(new[] { typeof(CuaHangDbContext) }, typeof(CuaHangDbContext).Assembly
            .GetTypes().Where(t => !t.IsAbstract && typeof(DbContext).IsAssignableFrom(t)).ToArray());
        using var db = Context();
        var model = db.Model.GetRelationalModel();
        Assert.Equal(28, SchemaReference.Tables.Length);
        Assert.Equal(3, SchemaReference.Views.Length);
        Assert.Equal(SchemaReference.Tables, model.Tables.Select(t => t.Name).Order());
        Assert.Equal(SchemaReference.Views, model.Views.Select(v => v.Name).Order());
        Assert.All(model.Tables, table => Assert.NotNull(table.PrimaryKey));
        Assert.All(db.Model.GetEntityTypes().Where(e => e.GetViewName() != null),
            entity => Assert.Null(entity.FindPrimaryKey()));
    }

    [Fact]
    public void ExistingMigrationOwnsAllReferenceDdl_IncludingChecksViewsAndTriggers()
    {
        using var db = Context();
        Assert.Equal(new[] { "20261026120030_InitialCreate" }, db.Database.GetMigrations());
        var script = SchemaReference.Normalize(db.GetService<IMigrator>().GenerateScript());
        var commands = SchemaReference.CreateStatements().ToArray();
        Assert.Equal(41, commands.Length); // 28 tables + 3 views + 10 triggers
        Assert.Equal(53, SchemaReference.Checks.Length);
        foreach (var command in commands)
            Assert.Contains(SchemaReference.Normalize(command), script);
    }

    [Fact]
    public void CurrentModelHasNoPendingRelationalChangesAgainstExistingSnapshot()
    {
        using var db = Context();
        Assert.False(db.Database.HasPendingModelChanges());
    }
}
