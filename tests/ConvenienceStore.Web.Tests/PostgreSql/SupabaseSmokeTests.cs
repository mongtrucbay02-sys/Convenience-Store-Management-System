using System.Linq.Expressions;
using System.Text.Json;
using ConvenienceStore.Web.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SmartMart.SupabaseSmoke;

namespace ConvenienceStore.Web.Tests.PostgreSql;

public sealed class SupabaseSmokeTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "smartmart-smoke-tests-" + Guid.NewGuid().ToString("N"));
    private string ReportPath => Path.Combine(_directory, "report.json");
    private string CertificatePath => Path.Combine(_directory, "ca.crt");
    private const string TestPassword = "synthetic-password-must-not-appear";

    public SupabaseSmokeTests()
    {
        Directory.CreateDirectory(_directory);
        // Configuration checks only file existence. Trust/hostname validation belongs to Npgsql.
        File.WriteAllText(CertificatePath, "synthetic file; not a trusted certificate");
    }

    private string Remote(SslMode mode = SslMode.VerifyFull, string? certificate = null) =>
        new NpgsqlConnectionStringBuilder
        {
            Host = "database.example.test", Database = "unused", Username = "fixture",
            Password = TestPassword, SslMode = mode, RootCertificate = certificate ?? CertificatePath
        }.ConnectionString;

    private static CuaHangDbContext CreateModel() => new(new DbContextOptionsBuilder<CuaHangDbContext>()
        .UseNpgsql("Host=127.0.0.1;Database=unused;Username=unused", pg => pg.SetPostgresVersion(17, 0)).Options);

    private static IReadOnlyList<SmokeObject> MappedObjects()
    {
        using var db = CreateModel();
        return SmokeMapping.FromModel(db.Model);
    }

    [Fact]
    public void RemoteVerifyFull_PreservesConfiguredCaAndEnforcesSafeguards()
    {
        var report = new SmokeReport();
        var settings = SmokeTlsPolicy.Configure(Remote(), report);
        Assert.Equal(SslMode.VerifyFull, settings.SslMode);
        Assert.Equal(CertificatePath, settings.RootCertificate);
        Assert.True(report.CertificateConfigured);
        Assert.True(report.CertificateFileExists);
        Assert.Equal("VerifyFull", report.TlsMode);
        Assert.Equal(GssEncryptionMode.Disable, settings.GssEncryptionMode);
        Assert.False(settings.Pooling);
        Assert.False(settings.Multiplexing);
        Assert.False(settings.IncludeErrorDetail);
        Assert.False(settings.LogParameters);
        Assert.False(settings.PersistSecurityInfo);
        Assert.Equal(25, settings.Timeout);
        Assert.Equal(20, settings.CommandTimeout);
        Assert.Equal("smartmart", settings.SearchPath);
        Assert.Equal("-c default_transaction_read_only=on", settings.Options);
    }

    [Theory]
    [InlineData(SslMode.Require)]
    [InlineData(SslMode.Prefer)]
    [InlineData(SslMode.Disable)]
    [InlineData(SslMode.Allow)]
    [InlineData(SslMode.VerifyCA)]
    public async Task RemoteWeakerModes_AreRejectedBeforeConnection(SslMode mode)
    {
        var result = await Run(Remote(mode));
        Assert.Equal(1, result.Exit);
        Assert.False(result.Opened);
        Assert.Equal("RemoteTlsRequiresVerifyFull", result.Json.GetProperty("reasonCode").GetString());
        Assert.Equal(mode.ToString(), result.Json.GetProperty("tlsMode").GetString());
    }

    [Theory]
    [InlineData("", "MissingRootCertificate")]
    [InlineData("missing-ca.crt", "RootCertificateNotFound")]
    public async Task RemoteMissingCertificate_IsRejectedBeforeConnection(string file, string reason)
    {
        var path = file.Length == 0 ? "" : Path.Combine(_directory, file);
        var result = await Run(Remote(certificate: path));
        Assert.Equal(1, result.Exit);
        Assert.False(result.Opened);
        Assert.Equal(reason, result.Json.GetProperty("reasonCode").GetString());
        Assert.Equal(file.Length != 0, result.Json.GetProperty("certificateConfigured").GetBoolean());
        Assert.False(result.Json.GetProperty("certificateFileExists").GetBoolean());
    }

    [Fact]
    public async Task ExplicitCertificateBypass_IsRejected()
    {
        var result = await Run(Remote() + ";Trust Server Certificate=true");
        Assert.Equal(1, result.Exit);
        Assert.False(result.Opened);
        Assert.Equal("CertificateBypassNotAllowed", result.Json.GetProperty("reasonCode").GetString());
    }

    [Theory]
    [InlineData("localhost")]
    [InlineData("LOCALHOST")]
    [InlineData("127.0.0.1")]
    [InlineData("127.0.0.2")]
    [InlineData("127.1")]
    [InlineData("2130706433")]
    [InlineData("::1")]
    public void LoopbackDisable_StillWorksWithoutCa(string host)
    {
        var settings = SmokeTlsPolicy.Configure(new NpgsqlConnectionStringBuilder
        { Host = host, SslMode = SslMode.Disable }.ConnectionString, new SmokeReport());
        Assert.Equal(SslMode.Disable, settings.SslMode);
        Assert.False(settings.Pooling);
    }

    [Theory]
    [InlineData("localhost.example.test")]
    [InlineData("127.0.0.1,database.example.test")]
    [InlineData("192.0.2.1")]
    [InlineData("::ffff:192.0.2.1")]
    [InlineData("127.0.0.1.example.test")]
    public void RemoteOrMixedHosts_CannotUseLoopbackException(string host)
    {
        var settings = new NpgsqlConnectionStringBuilder { Host = host, SslMode = SslMode.Disable };
        var failure = Assert.Throws<SmokeValidationException>(() => SmokeTlsPolicy.Configure(settings.ConnectionString, new SmokeReport()));
        Assert.Equal("RemoteTlsRequiresVerifyFull", failure.Code);
    }

    [Fact]
    public async Task MissingConfiguration_ProducesSafeJsonWithoutConnecting()
    {
        var result = await Run(null);
        Assert.Equal(2, result.Exit);
        Assert.False(result.Opened);
        Assert.Equal("MissingConnection", result.Json.GetProperty("reasonCode").GetString());
        Assert.False(result.Json.GetProperty("readOnly").GetBoolean());
    }

    [Fact]
    public async Task MalformedConnection_DoesNotLeakParsingExceptionOrInput()
    {
        var input = "Password=" + TestPassword + ";secret-unsupported-key=" + TestPassword;
        var result = await Run(input);
        Assert.Equal(1, result.Exit);
        Assert.False(result.Opened);
        Assert.DoesNotContain(input, result.Text);
        Assert.DoesNotContain(TestPassword, result.Text);
        Assert.DoesNotContain("secret-unsupported-key", result.Text);
    }

    [Fact]
    public async Task QueryError_DoesNotLeakExceptionConnectionCaPathOrRowValues()
    {
        var session = new FakeSession { FailAt = 3, Failure = new InvalidOperationException(Remote() + " secret-row-value") };
        var result = await Run(Remote(), session);
        Assert.Equal(1, result.Exit);
        Assert.DoesNotContain(Remote(), result.Text);
        Assert.DoesNotContain(TestPassword, result.Text);
        Assert.DoesNotContain(CertificatePath, result.Text);
        Assert.DoesNotContain("secret-row-value", result.Text);
        Assert.Equal(2, result.Json.GetProperty("checkedObjects").GetInt32());
        Assert.Equal(session.Plan[2].QualifiedName, result.Json.GetProperty("failedObject").GetString());
        Assert.Equal(1, session.Rollbacks);
        Assert.True(session.Disposed);
    }

    [Fact]
    public async Task ReadOnlyOff_PreventsEveryMappedQueryAndRollsBack()
    {
        var session = new FakeSession { ReadOnly = false };
        var result = await Run(Remote(), session);
        Assert.Equal(1, result.Exit);
        Assert.Equal("ReadOnlyGuardNotApplied", result.Json.GetProperty("reasonCode").GetString());
        Assert.Equal(0, session.PlanReads);
        Assert.Empty(session.Reads);
        Assert.Equal(1, session.Rollbacks);
        Assert.True(session.Disposed);
    }

    [Fact]
    public async Task GuardFailure_StillAttemptsRollbackAndDisposesSession()
    {
        var session = new FakeSession { BeginFailure = new InvalidOperationException(TestPassword) };
        var result = await Run(Remote(), session);
        Assert.Equal(1, result.Exit);
        Assert.Empty(session.Reads);
        Assert.Equal(1, session.Rollbacks);
        Assert.True(session.Disposed);
        Assert.DoesNotContain(TestPassword, result.Text);
    }

    [Fact]
    public async Task SuccessfulRun_ChecksAll34MappingsAfterGuardThenRollsBack()
    {
        var session = new FakeSession();
        var result = await Run(Remote(), session);
        Assert.Equal(0, result.Exit);
        Assert.Equal("passed", result.Json.GetProperty("status").GetString());
        Assert.True(result.Json.GetProperty("readOnly").GetBoolean());
        Assert.Equal(31, result.Json.GetProperty("tables").GetInt32());
        Assert.Equal(3, result.Json.GetProperty("views").GetInt32());
        Assert.Equal(34, result.Json.GetProperty("checkedObjects").GetInt32());
        Assert.Equal(0, result.Json.GetProperty("writes").GetInt32());
        Assert.Equal(session.Plan.Select(p => p.QualifiedName), session.Reads);
        Assert.Equal("begin-read-only", session.Events[0]);
        Assert.Equal("mapping", session.Events[1]);
        Assert.Equal("rollback", session.Events[^2]);
        Assert.Equal("dispose", session.Events[^1]);
        Assert.Equal(1, session.Rollbacks);
    }

    [Theory]
    [InlineData("wrong-schema")]
    [InlineData("missing-object")]
    [InlineData("duplicate-object")]
    public async Task InvalidMapping_FailsBeforeAnyObjectIsRead(string error)
    {
        var objects = MappedObjects().ToArray();
        if (error == "wrong-schema") objects[0] = objects[0] with { Schema = "public" };
        else if (error == "missing-object") objects = objects[1..];
        else objects[0] = objects[1];
        var session = new FakeSession { Plan = objects };
        var result = await Run(Remote(), session);
        Assert.Equal(1, result.Exit);
        Assert.Equal("UnexpectedMapping", result.Json.GetProperty("reasonCode").GetString());
        Assert.Empty(session.Reads);
        Assert.Equal(1, session.Rollbacks);
    }

    [Fact]
    public void RealEfQueries_CheckContractAndUseNoTrackingTakeOneForEveryObject()
    {
        using var db = CreateModel();
        var objects = SmokeMapping.FromModel(db.Model);
        SmokeMapping.Validate(objects);
        using var catalog = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,
            "PostgreSql", "supabase-schema-contract.json")));
        var expected = catalog.RootElement.GetProperty("tables").EnumerateArray().Select(t => t.GetProperty("name").GetString())
            .Concat(catalog.RootElement.GetProperty("views").EnumerateArray().Select(v => v.GetProperty("name").GetString()));
        Assert.Equal(expected.OrderBy(n => n, StringComparer.Ordinal), objects.Select(o => o.Name).OrderBy(n => n, StringComparer.Ordinal));
        foreach (var mappedObject in objects)
        {
            var query = SmokeReader.BuildQuery(db, mappedObject);
            var take = Assert.IsType<MethodCallExpression>(query.Expression, exactMatch: false);
            Assert.Equal("Take", take.Method.Name);
            Assert.Equal(1, Assert.IsType<ConstantExpression>(take.Arguments[1], exactMatch: false).Value);
            var noTracking = Assert.IsType<MethodCallExpression>(take.Arguments[0], exactMatch: false);
            Assert.Equal("AsNoTracking", noTracking.Method.Name);
            var sql = query.ToQueryString();
            Assert.Contains("smartmart.", sql);
            Assert.Contains(mappedObject.Name, sql);
            Assert.Contains("LIMIT", sql);
        }
    }

    [Fact]
    public async Task RollbackFailure_CannotProducePassedReport()
    {
        var session = new FakeSession { FailRollback = true };
        var result = await Run(Remote(), session);
        Assert.Equal(1, result.Exit);
        Assert.Equal("failed", result.Json.GetProperty("status").GetString());
        Assert.Equal(1, session.Rollbacks);
        Assert.True(session.Disposed);
    }

    [Fact]
    public async Task ExistingReport_IsPreservedAndNoConnectionIsAttempted()
    {
        File.WriteAllText(ReportPath, "previous evidence");
        var opened = false;
        using var output = new StringWriter();
        var exit = await SmokeRunner.RunAsync(Remote(), ReportPath, output, output, _ =>
        { opened = true; return new FakeSession(); });
        Assert.Equal(2, exit);
        Assert.False(opened);
        Assert.Equal("previous evidence", File.ReadAllText(ReportPath));
        Assert.DoesNotContain(TestPassword, output.ToString());
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task ResourceCleanup_AttemptsEveryDisposeEvenWhenEarlierCleanupFails(bool failContext, bool failTransaction)
    {
        var events = new List<string>();
        var context = new TrackedResource("context", events, failContext);
        var transaction = new TrackedResource("transaction", events, failTransaction);
        var connection = new TrackedResource("connection", events, false);
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await PostgreSqlSmokeSession.DisposeResourcesAsync(context, transaction, connection));
        Assert.Equal(new[] { "context", "transaction", "connection" }, events);
    }

    [Fact]
    public async Task SessionDisposeFailure_CannotLeaveAPassedVerdictOrLeakItsMessage()
    {
        var session = new FakeSession { FailDispose = true };
        var result = await Run(Remote(), session);
        Assert.Equal(1, result.Exit);
        Assert.Equal("failed", result.Json.GetProperty("status").GetString());
        Assert.Equal(34, result.Json.GetProperty("checkedObjects").GetInt32());
        Assert.Equal(1, session.Rollbacks);
        Assert.True(session.Disposed);
        Assert.DoesNotContain(TestPassword, result.Text);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CancellationDuringGuardOrQuery_FailsAndStillRollsBack(bool duringGuard)
    {
        var cancelled = new OperationCanceledException(TestPassword);
        var session = new FakeSession
        {
            BeginFailure = duringGuard ? cancelled : null,
            FailAt = duringGuard ? 0 : 1, Failure = cancelled
        };
        var result = await Run(Remote(), session);
        Assert.Equal(1, result.Exit);
        Assert.Equal("failed", result.Json.GetProperty("status").GetString());
        Assert.Equal(0, result.Json.GetProperty("checkedObjects").GetInt32());
        Assert.Equal(duringGuard ? 0 : 1, session.Reads.Count);
        Assert.Equal(1, session.Rollbacks);
        Assert.True(session.Disposed);
        Assert.DoesNotContain(TestPassword, result.Text);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task InvalidReportLocation_FailsBeforeConnection(bool targetIsDirectory)
    {
        var path = _directory;
        if (!targetIsDirectory)
        {
            var blockingFile = Path.Combine(_directory, "not-a-directory");
            File.WriteAllText(blockingFile, "preserve");
            path = Path.Combine(blockingFile, "report.json");
        }
        var opened = false;
        using var output = new StringWriter();
        var exit = await SmokeRunner.RunAsync(Remote(), path, output, output, _ =>
        { opened = true; return new FakeSession(); });
        Assert.Equal(2, exit);
        Assert.False(opened);
        Assert.DoesNotContain(TestPassword, output.ToString());
        Assert.DoesNotContain(CertificatePath, output.ToString());
    }

    private sealed class TrackedResource(string name, List<string> events, bool fail) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            events.Add(name);
            return fail ? ValueTask.FromException(new InvalidOperationException("fixture cleanup failure")) : ValueTask.CompletedTask;
        }
    }

    private async Task<(int Exit, JsonElement Json, string Text, bool Opened)> Run(string? input, FakeSession? session = null)
    {
        using var output = new StringWriter();
        using var errors = new StringWriter();
        var opened = false;
        var exit = await SmokeRunner.RunAsync(input, ReportPath, output, errors, _ =>
        { opened = true; return session ?? new FakeSession(); });
        var json = File.ReadAllText(ReportPath);
        using var document = JsonDocument.Parse(json);
        var text = json + output + errors.ToString();
        Assert.DoesNotContain(TestPassword, text);
        return (exit, document.RootElement.Clone(), text, opened);
    }

    public void Dispose()
    {
        foreach (var file in Directory.EnumerateFiles(_directory)) File.Delete(file);
        Directory.Delete(_directory);
    }

    private sealed class FakeSession : ISmokeSession
    {
        internal IReadOnlyList<SmokeObject> Plan { get; init; } = MappedObjects();
        internal bool ReadOnly { get; init; } = true;
        internal Exception? BeginFailure { get; init; }
        internal Exception? Failure { get; init; }
        internal int FailAt { get; init; }
        internal bool FailRollback { get; init; }
        internal bool FailDispose { get; init; }
        internal int PlanReads { get; private set; }
        internal int Rollbacks { get; private set; }
        internal bool Disposed { get; private set; }
        internal List<string> Reads { get; } = [];
        internal List<string> Events { get; } = [];
        public IReadOnlyList<SmokeObject> Objects { get { PlanReads++; Events.Add("mapping"); return Plan; } }
        public Task<bool> BeginReadOnlyAsync()
        {
            Events.Add("begin-read-only");
            return BeginFailure is null ? Task.FromResult(ReadOnly) : Task.FromException<bool>(BeginFailure);
        }
        public Task<int> ProbeAsync(SmokeObject mappedObject)
        {
            Reads.Add(mappedObject.QualifiedName); Events.Add(mappedObject.QualifiedName);
            return Reads.Count == FailAt ? Task.FromException<int>(Failure ?? new InvalidOperationException("fixture failure")) : Task.FromResult(1);
        }
        public Task RollbackAsync()
        {
            Rollbacks++; Events.Add("rollback");
            return FailRollback ? Task.FromException(new InvalidOperationException("fixture rollback failure")) : Task.CompletedTask;
        }
        public ValueTask DisposeAsync()
        {
            Disposed = true; Events.Add("dispose");
            return FailDispose ? ValueTask.FromException(new InvalidOperationException(TestPassword)) : ValueTask.CompletedTask;
        }
    }
}
