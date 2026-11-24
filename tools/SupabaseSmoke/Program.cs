using System.Reflection;
using System.Text.Json;
using ConvenienceStore.Web.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

// This probe never starts Program.cs, authentication, seeders or migrations.
// It shares one explicitly read-only transaction across every EF query.
var reportPath = Path.GetFullPath(args.FirstOrDefault() ?? "artifacts/supabase-smoke/result.json");
Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
var probes = new List<ObjectProbe>();
var readOnly = false;
string? currentObject = null;
var input = Environment.GetEnvironmentVariable("SUPABASE_DB_CONNECTION");
if (string.IsNullOrWhiteSpace(input))
{
    await Save(new { status = "failed", reason = "Missing SUPABASE_DB_CONNECTION", readOnly, probes });
    Console.Error.WriteLine("Thiếu GitHub Secret SUPABASE_DB_CONNECTION. Chưa kết nối hoặc chạy kiểm tra.");
    return 2;
}
try
{
    var settings = new NpgsqlConnectionStringBuilder(input)
    {
        Pooling = false, Multiplexing = false, SearchPath = "smartmart",
        Timezone = "Asia/Ho_Chi_Minh", Timeout = 25, CommandTimeout = 20,
        Options = "-c default_transaction_read_only=on", IncludeErrorDetail = false,
        ApplicationName = "SmartMart-Supabase-ReadOnly-Smoke"
    };
    if (settings.Host is not ("localhost" or "127.0.0.1" or "::1"))
        settings.SslMode = SslMode.Require;
    await using var connection = new NpgsqlConnection(settings.ConnectionString);
    await connection.OpenAsync();
    await using var transaction = await connection.BeginTransactionAsync();
    // Poolers can ignore startup options, so enforce and verify inside the transaction.
    await using (var protect = new NpgsqlCommand(
        "SET TRANSACTION READ ONLY; SET LOCAL statement_timeout='15s'; SET LOCAL lock_timeout='3s';", connection, transaction))
        await protect.ExecuteNonQueryAsync();
    await using (var check = new NpgsqlCommand("SHOW transaction_read_only", connection, transaction))
        readOnly = (string?)await check.ExecuteScalarAsync() == "on";
    if (!readOnly) throw new InvalidOperationException("Read-only guard was not applied.");
    await using var db = new CuaHangDbContext(new DbContextOptionsBuilder<CuaHangDbContext>()
        .UseNpgsql(connection, pg => pg.SetPostgresVersion(17, 0)).Options);
    await db.Database.UseTransactionAsync(transaction);
    var entities = db.Model.GetEntityTypes()
        .Where(e => e.GetTableName() != null || e.GetViewName() != null)
        .OrderBy(e => e.GetTableName() ?? e.GetViewName()).ToArray();
    var tables = entities.Where(e => e.GetTableName() != null)
        .Select(e => (e.GetSchema(), e.GetTableName())).Distinct().Count();
    var views = entities.Where(e => e.GetViewName() != null)
        .Select(e => (e.GetViewSchema(), e.GetViewName())).Distinct().Count();
    if (tables != 31 || views != 3)
        throw new InvalidOperationException("Expected 31 mapped tables and 3 views; update the reviewed contract first.");
    var method = typeof(Reader).GetMethod(nameof(Reader.Probe))!;
    foreach (var entity in entities)
    {
        var schema = entity.GetSchema() ?? entity.GetViewSchema();
        if (schema != "smartmart") throw new InvalidOperationException("Refusing a schema outside smartmart.");
        currentObject = schema + "." + (entity.GetTableName() ?? entity.GetViewName());
        var rows = entity.HasSharedClrType
            ? (await db.Set<Dictionary<string, object>>(entity.Name).AsNoTracking().Take(1).ToListAsync()).Count
            : await (Task<int>)method.MakeGenericMethod(entity.ClrType).Invoke(null, [db])!;
        probes.Add(new(currentObject, entity.GetViewName() == null ? "table" : "view", rows));
        Console.WriteLine("PASS " + currentObject);
    }
    await transaction.RollbackAsync();
    await Save(new { status = "passed", provider = "PostgreSql", readOnly,
        tables, views, checkedObjects = probes.Count, writes = 0,
        checkedAtUtc = DateTimeOffset.UtcNow, probes });
    Console.WriteLine($"Supabase read-only smoke: {tables} bảng, {views} view; không ghi dữ liệu.");
    return 0;
}
catch (Exception error)
{
    while (error is TargetInvocationException { InnerException: not null }) error = error.InnerException;
    // Do not emit exception messages/stack traces: database errors can contain data or connection details.
    var sqlState = error is PostgresException pg ? pg.SqlState : null;
    await Save(new { status = "failed", readOnly, failedObject = currentObject,
        errorType = error.GetType().Name, sqlState, checkedObjects = probes.Count, probes });
    Console.Error.WriteLine("Supabase smoke thất bại: " + error.GetType().Name +
        (sqlState == null ? "" : " SQLSTATE=" + sqlState) +
        (currentObject == null ? "" : " object=" + currentObject));
    return 1;
}
async Task Save(object result) => await File.WriteAllTextAsync(reportPath,
    JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));

sealed record ObjectProbe(string Name, string Kind, int RowsRead);
static class Reader
{
    public static async Task<int> Probe<T>(CuaHangDbContext db) where T : class =>
        (await db.Set<T>().AsNoTracking().Take(1).ToListAsync()).Count;
}
