using System.Reflection;
using System.Text.Json;
using ConvenienceStore.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql;

namespace SmartMart.SupabaseSmoke;

internal sealed class SmokeReport
{
    public string Status { get; set; } = "failed";
    public string? ReasonCode { get; set; }
    public string? ErrorType { get; set; }
    public string? SqlState { get; set; }
    public string? CauseType { get; set; }
    public string? FailedObject { get; set; }
    public string? TlsMode { get; set; }
    public bool CertificateConfigured { get; set; }
    public bool CertificateFileExists { get; set; }
    public bool ReadOnly { get; set; }
    public bool RoleVerified { get; set; }
    public string? CurrentUser { get; set; }
    public string? SessionUser { get; set; }
    public bool? IsSuperuser { get; set; }
    public bool? BypassRls { get; set; }
    public bool? CanCreateRole { get; set; }
    public bool? CanCreateDatabase { get; set; }
    public bool? RowSecurity { get; set; }
    public int Tables { get; set; }
    public int Views { get; set; }
    public int CheckedObjects => Probes.Count;
    public int Writes => 0;
    public DateTimeOffset CheckedAtUtc { get; set; }
    public List<ObjectProbe> Probes { get; } = [];
}

internal sealed record ObjectProbe(string Name, string Kind, int RowsRead);
internal sealed record SmokeObject(string Schema, string Name, string Kind, Type ClrType,
    string? SharedEntityName = null)
{
    internal string QualifiedName => Schema + "." + Name;
}

internal static class SmokeMapping
{
    internal static IReadOnlyList<SmokeObject> FromModel(IModel model) => model.GetEntityTypes()
        .Where(e => e.GetTableName() is not null || e.GetViewName() is not null)
        .Select(e => new SmokeObject(
            (e.GetViewName() is not null ? e.GetViewSchema() : e.GetSchema()) ?? "",
            e.GetViewName() ?? e.GetTableName()!, e.GetViewName() is not null ? "view" : "table",
            e.ClrType, e.HasSharedClrType ? e.Name : null))
        .OrderBy(e => e.QualifiedName, StringComparer.Ordinal).ToArray();

    internal static void Validate(IReadOnlyList<SmokeObject> objects)
    {
        if (objects.Any(e => e.Schema != "smartmart" || e.Kind is not ("table" or "view"))
            || objects.Count(e => e.Kind == "table") != 31
            || objects.Count(e => e.Kind == "view") != 3
            || objects.Select(e => e.QualifiedName).Distinct(StringComparer.Ordinal).Count() != 34)
            throw new SmokeValidationException("UnexpectedMapping");
    }
}

internal interface ISmokeSession : IAsyncDisposable
{
    Task<bool> BeginReadOnlyAsync();
    Task<SmokeRoleEvidence> ReadRoleAsync();
    IReadOnlyList<SmokeObject> Objects { get; }
    Task<int> ProbeAsync(SmokeObject mappedObject);
    Task RollbackAsync();
}

internal static class SmokeRunner
{
    internal static async Task<int> RunAsync(string? input, string reportPath,
        TextWriter output, TextWriter errorOutput,
        Func<NpgsqlConnectionStringBuilder, ISmokeSession>? createSession = null)
    {
        // Reserve the evidence path before connecting. Never overwrite an earlier report.
        try
        {
            var path = Path.GetFullPath(reportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            var report = new SmokeReport();
            var exit = 1;
            try
            {
                if (string.IsNullOrWhiteSpace(input))
                    throw new SmokeValidationException("MissingConnection");
                var settings = SmokeTlsPolicy.Configure(input, report);
                await using var session = createSession is null
                    ? new PostgreSqlSmokeSession(settings) : createSession(settings);
                try
                {
                    report.ReadOnly = await session.BeginReadOnlyAsync();
                    if (!report.ReadOnly) throw new SmokeValidationException("ReadOnlyGuardNotApplied");
                    SmokeRoleGuard.Verify(await session.ReadRoleAsync(), report);
                    var objects = session.Objects;
                    SmokeMapping.Validate(objects);
                    report.Tables = objects.Count(e => e.Kind == "table");
                    report.Views = objects.Count(e => e.Kind == "view");
                    foreach (var mappedObject in objects)
                    {
                        report.FailedObject = mappedObject.QualifiedName;
                        var rows = await session.ProbeAsync(mappedObject);
                        if (rows is < 0 or > 1) throw new SmokeValidationException("InvalidProbeRowCount");
                        report.Probes.Add(new(mappedObject.QualifiedName, mappedObject.Kind, rows));
                        await output.WriteLineAsync("PASS " + mappedObject.QualifiedName);
                    }
                    report.FailedObject = null;
                }
                finally
                {
                    // Also attempt explicit rollback after guard/query/mapping failures.
                    await session.RollbackAsync();
                }
                report.Status = "passed";
                exit = 0;
            }
            catch (Exception exception)
            {
                while (exception is TargetInvocationException { InnerException: not null })
                    exception = exception.InnerException;
                report.Status = "failed";
                report.ErrorType = exception.GetType().Name;
                report.CauseType = exception.GetBaseException().GetType().Name;
                report.ReasonCode = exception is SmokeValidationException validation ? validation.Code : "ProbeFailed";
                report.SqlState = exception is PostgresException pg ? pg.SqlState : null;
                exit = report.ReasonCode == "MissingConnection" ? 2 : 1;
                // Never emit exception messages, stack traces, settings, CA paths or row data.
                await errorOutput.WriteLineAsync("Supabase smoke failed: " + report.ReasonCode
                    + " (" + report.ErrorType + ")");
            }
            report.CheckedAtUtc = DateTimeOffset.UtcNow;
            await JsonSerializer.SerializeAsync(file, report, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true
            });
            return exit;
        }
        catch (Exception exception)
        {
            // An existing/unwritable evidence path must fail before any database connection.
            await errorOutput.WriteLineAsync("Supabase smoke report unavailable (" + exception.GetType().Name
                + "). Choose a new writable report path; existing reports are never overwritten.");
            return 2;
        }
    }
}

internal sealed class PostgreSqlSmokeSession : ISmokeSession
{
    private readonly NpgsqlConnection _connection;
    private NpgsqlTransaction? _transaction;
    private CuaHangDbContext? _db;
    private bool _readOnly;

    internal PostgreSqlSmokeSession(NpgsqlConnectionStringBuilder settings) =>
        _connection = new NpgsqlConnection(settings.ConnectionString);

    public IReadOnlyList<SmokeObject> Objects => SmokeMapping.FromModel(_db!.Model);

    public async Task<bool> BeginReadOnlyAsync()
    {
        await _connection.OpenAsync();
        _transaction = await _connection.BeginTransactionAsync();
        // Poolers can ignore startup options: enforce and verify inside this transaction.
        await using (var protect = new NpgsqlCommand(
            "SET TRANSACTION READ ONLY; SET LOCAL statement_timeout='15s'; SET LOCAL lock_timeout='3s';",
            _connection, _transaction))
            await protect.ExecuteNonQueryAsync();
        await using (var check = new NpgsqlCommand("SHOW transaction_read_only", _connection, _transaction))
            _readOnly = (string?)await check.ExecuteScalarAsync() == "on";
        if (!_readOnly) return false;
        _db = new CuaHangDbContext(new DbContextOptionsBuilder<CuaHangDbContext>()
            .UseNpgsql(_connection, pg => pg.SetPostgresVersion(17, 0)).Options);
        await _db.Database.UseTransactionAsync(_transaction);
        return true;
    }

    public async Task<SmokeRoleEvidence> ReadRoleAsync()
    {
        if (!_readOnly || _transaction is null)
            throw new SmokeValidationException("ReadOnlyGuardNotApplied");
        await using var command = new NpgsqlCommand(SmokeRoleGuard.Query, _connection, _transaction);
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            throw new SmokeValidationException("RoleEvidenceUnavailable");
        var evidence = new SmokeRoleEvidence(reader.GetString(0), reader.GetString(1),
            reader.GetBoolean(2), reader.GetBoolean(3), reader.GetBoolean(4),
            reader.GetBoolean(5), reader.GetString(6) == "on", reader.GetString(7) == "on");
        if (await reader.ReadAsync())
            throw new SmokeValidationException("RoleEvidenceUnavailable");
        return evidence;
    }

    public async Task<int> ProbeAsync(SmokeObject mappedObject)
    {
        if (!_readOnly || _db is null || mappedObject.Schema != "smartmart")
            throw new SmokeValidationException("ReadOnlyGuardNotApplied");
        var rows = 0;
        await foreach (var ignored in (IAsyncEnumerable<object>)SmokeReader.BuildQuery(_db, mappedObject)) rows++;
        return rows;
    }

    public async Task RollbackAsync()
    {
        if (_transaction?.Connection is not null) await _transaction.RollbackAsync();
    }

    public ValueTask DisposeAsync() => DisposeResourcesAsync(_db, _transaction, _connection);

    internal static async ValueTask DisposeResourcesAsync(IAsyncDisposable? context,
        IAsyncDisposable? transaction, IAsyncDisposable connection)
    {
        try
        {
            if (context is not null) await context.DisposeAsync();
        }
        finally
        {
            try
            {
                if (transaction is not null) await transaction.DisposeAsync();
            }
            finally
            {
                // A failed earlier cleanup must not leave the physical connection open.
                await connection.DisposeAsync();
            }
        }
    }
}

internal static class SmokeReader
{
    internal static IQueryable BuildQuery(CuaHangDbContext db, SmokeObject mappedObject) =>
        mappedObject.SharedEntityName is not null
            ? db.Set<Dictionary<string, object>>(mappedObject.SharedEntityName).AsNoTracking().Take(1)
            : (IQueryable)typeof(SmokeReader).GetMethod(nameof(TypedQuery), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(mappedObject.ClrType).Invoke(null, [db])!;

    private static IQueryable<T> TypedQuery<T>(CuaHangDbContext db) where T : class =>
        db.Set<T>().AsNoTracking().Take(1);
}
