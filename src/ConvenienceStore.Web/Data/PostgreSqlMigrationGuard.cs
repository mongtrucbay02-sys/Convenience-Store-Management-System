using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ConvenienceStore.Web.Data;

// Existing migrations describe MySQL. A reviewed PostgreSQL baseline is required
// before this context may generate or apply PostgreSQL migrations.
public sealed class PostgreSqlMigrationGuard : IMigrator
{
    private static InvalidOperationException Unsupported() => new(
        "Các migration hiện tại dành cho MySQL. Chưa có baseline PostgreSQL được duyệt; " +
        "không áp migration MySQL lên Supabase.");

    public void Migrate(string? targetMigration = null) => throw Unsupported();
    public Task MigrateAsync(string? targetMigration = null, CancellationToken cancellationToken = default)
        => throw Unsupported();
    public string GenerateScript(string? fromMigration = null, string? toMigration = null,
        MigrationsSqlGenerationOptions options = MigrationsSqlGenerationOptions.Default) => throw Unsupported();
    public bool HasPendingModelChanges() => throw Unsupported();
}

public partial class CuaHangDbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.Options.Extensions.Any(extension =>
            extension.GetType().Assembly.GetName().Name == "Npgsql.EntityFrameworkCore.PostgreSQL"))
            optionsBuilder.ReplaceService<IMigrator, PostgreSqlMigrationGuard>();
        base.OnConfiguring(optionsBuilder);
    }
}
