using System.Text.Json;
using ConvenienceStore.Web.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SmartMart.SupabaseSmoke;

namespace ConvenienceStore.Web.Tests.PostgreSql;

// Separate PostgreSQL service: roles are cluster-wide, not database-local.
public sealed class SmokeRolePostgreSqlFixture : IAsyncLifetime
{
    private const string Role = "smartmart_backend";
    private readonly string _database = "smartmart_role_test_" + Guid.NewGuid().ToString("N");
    private readonly string _observedDatabase = "smartmart_role_test_" + Guid.NewGuid().ToString("N");
    private bool _observedDatabaseCreated;
    private readonly string _password = Guid.NewGuid().ToString("N");
    private string? _server;
    private bool _roleCreated;
    private bool _databaseCreated;

    public async Task InitializeAsync()
    {
        var input = Environment.GetEnvironmentVariable("ConnectionStrings__CuaHangPostgreSqlRoleTest")
            ?? throw new InvalidOperationException("Dedicated loopback PostgreSQL 17 role test service is required.");
        var businessInput = Environment.GetEnvironmentVariable("ConnectionStrings__CuaHangPostgreSqlTest")
            ?? throw new InvalidOperationException("Business test service is required to verify isolation.");
        var settings = new NpgsqlConnectionStringBuilder(input);
        var business = new NpgsqlConnectionStringBuilder(businessInput);
        if (settings.Host != "127.0.0.1" || settings.Port == business.Port ||
            settings.Database != "postgres" || settings.Username != "postgres")
            throw new InvalidOperationException("Refuse role tests outside the dedicated loopback instance.");
        settings.Pooling = false;
        settings.Options = "";
        await using var server = new NpgsqlConnection(settings.ConnectionString);
        await server.OpenAsync();
        await using (var check = new NpgsqlCommand("""
            SELECT current_setting('server_version_num')::int / 10000 = 17
               AND current_user = 'postgres'
               AND session_user = 'postgres'
               AND NOT EXISTS (SELECT 1 FROM pg_database WHERE datname NOT IN ('postgres','template0','template1'))
               AND NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'smartmart_backend')
            """, server))
            if (await check.ExecuteScalarAsync() is not true)
                throw new InvalidOperationException("Role test cluster is not a fresh PostgreSQL 17 instance.");

        _server = settings.ConnectionString;
        try
        {
            // Generated password contains only hex digits; database name contains only safe identifier characters.
            await Execute(server, $"CREATE ROLE {Role} LOGIN NOSUPERUSER NOBYPASSRLS NOCREATEROLE NOCREATEDB NOINHERIT PASSWORD '{_password}'");
            _roleCreated = true;
            await Execute(server, $"CREATE DATABASE {_database}");
            _databaseCreated = true;
            await using var db = new CuaHangDbContext(new DbContextOptionsBuilder<CuaHangDbContext>()
                .UseNpgsql(AdminConnection, pg => pg.SetPostgresVersion(17, 0)).Options);
            await db.Database.MigrateAsync();
            await db.Database.ExecuteSqlRawAsync("INSERT INTO smartmart.vai_tro(id,ma,ten) VALUES (1,'AD','Admin test')");
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    internal string AdminConnection => new NpgsqlConnectionStringBuilder(_server!)
        { Database = _database, SearchPath = "smartmart", Pooling = false }.ConnectionString;

    internal string BackendConnection => BackendFor(AdminConnection);

    private string ObservedAdminConnection => new NpgsqlConnectionStringBuilder(AdminConnection)
        { Database = _observedDatabase }.ConnectionString;

    internal string ObservedBackendConnection => BackendFor(ObservedAdminConnection);

    private string BackendFor(string adminConnection) => new NpgsqlConnectionStringBuilder(adminConnection)
        { Username = Role, Password = _password, SslMode = SslMode.Disable }.ConnectionString;

    internal async Task AssertSafeRoleAsync()
    {
        await using var connection = new NpgsqlConnection(BackendConnection);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            SELECT current_user = 'smartmart_backend'
               AND session_user = 'smartmart_backend'
               AND current_setting('row_security') = 'on'
               AND rolcanlogin AND NOT rolsuper AND NOT rolbypassrls
               AND NOT rolcreaterole AND NOT rolcreatedb AND NOT rolreplication AND NOT rolinherit
            FROM pg_roles WHERE rolname = current_user
            """, connection);
        Assert.True(await command.ExecuteScalarAsync() is true, "Runtime role was not restored to its safe state.");
    }

    internal async Task PrepareObservedDatabaseAsync()
    {
        await AssertSafeRoleAsync();
        if (_observedDatabaseCreated)
            throw new InvalidOperationException("Observed database must be prepared only once per owned lifetime.");
        await using var server = new NpgsqlConnection(_server!);
        await server.OpenAsync();
        await Execute(server, $"CREATE DATABASE {_observedDatabase}");
        _observedDatabaseCreated = true;
        try
        {
            await using var db = new CuaHangDbContext(new DbContextOptionsBuilder<CuaHangDbContext>()
                .UseNpgsql(ObservedAdminConnection, pg => pg.SetPostgresVersion(17, 0)).Options);
            await db.Database.MigrateAsync();
            await db.Database.ExecuteSqlRawAsync("INSERT INTO smartmart.vai_tro(id,ma,ten) VALUES (1,'AD','Admin test')");
            await ApplyObservedAclAsync(ObservedAdminConnection);
        }
        catch
        {
            await DropObservedDatabaseAsync();
            throw;
        }
    }

    internal async Task DropObservedDatabaseAsync()
    {
        if (!_observedDatabaseCreated) return;
        await using var server = new NpgsqlConnection(_server!);
        await server.OpenAsync();
        await Execute(server, $"DROP DATABASE IF EXISTS {_observedDatabase} WITH (FORCE)");
        _observedDatabaseCreated = false;
    }

    internal async Task<string> ReadBaselineAclFingerprintAsync()
    {
        await using var connection = new NpgsqlConnection(AdminConnection);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            SELECT jsonb_build_object(
                'objects', (SELECT jsonb_agg(jsonb_build_array(c.relname, c.relkind, c.relacl::text,
                    c.relrowsecurity, c.relforcerowsecurity) ORDER BY c.relname)
                    FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
                    WHERE n.nspname='smartmart' AND c.relkind IN ('r','p','v','S')),
                'policies', (SELECT jsonb_agg(to_jsonb(p) ORDER BY p.tablename,p.policyname)
                    FROM pg_policies p WHERE p.schemaname='smartmart')
            )::text
            """, connection);
        return (string)(await command.ExecuteScalarAsync())!;
    }

    internal async Task SetRoleStateAsync(string state)
    {
        // Only fixed literals; never accept arbitrary SQL or role names.
        var sql = state switch
        {
            "super" => "ALTER ROLE smartmart_backend SUPERUSER",
            "bypass" => "ALTER ROLE smartmart_backend BYPASSRLS",
            "create-role" => "ALTER ROLE smartmart_backend CREATEROLE",
            "create-db" => "ALTER ROLE smartmart_backend CREATEDB",
            "row-security" => "ALTER ROLE smartmart_backend SET row_security TO off",
            "reset" => "ALTER ROLE smartmart_backend NOSUPERUSER NOBYPASSRLS NOCREATEROLE NOCREATEDB; ALTER ROLE smartmart_backend RESET row_security",
            _ => throw new ArgumentOutOfRangeException(nameof(state))
        };
        await using var server = new NpgsqlConnection(_server!);
        await server.OpenAsync();
        await Execute(server, sql);
    }

    // Replays the B02 read-only ACL catalog in a disposable PG17 role-test database.
    // This is AS-IS, not the proposed D1-D7 target or an approved migration.
    private async Task ApplyObservedAclAsync(string adminConnection)
    {
        if (!_databaseCreated || !_roleCreated || _server is null)
            throw new InvalidOperationException("Disposable role fixture has not been initialized.");

        var snapshotPath = Path.Combine(AppContext.BaseDirectory,
            "SchemaReference", "observed-security-contract.json");
        using var snapshot = JsonDocument.Parse(await File.ReadAllTextAsync(snapshotPath));
        var root = snapshot.RootElement;
        if (root.GetProperty("role").GetProperty("rolname").GetString() != Role ||
            root.GetProperty("tables").GetArrayLength() != 31 ||
            root.GetProperty("views").GetArrayLength() != 3 ||
            root.GetProperty("sequences").GetArrayLength() != 20)
            throw new InvalidOperationException("Unexpected B02 security snapshot; refuse ACL replay.");

        static string Identifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                !System.Text.RegularExpressions.Regex.IsMatch(value, "^[a-z_][a-z0-9_]*$"))
                throw new InvalidOperationException("Unsafe identifier in ACL snapshot.");
            return "\"" + value + "\"";
        }
        static string Permissions(JsonElement entry, HashSet<string> allowed)
        {
            var values = entry.GetProperty("backend").EnumerateArray()
                .Select(value => value.GetString() ?? "").ToArray();
            if (values.Length == 0 || values.Any(value => !allowed.Contains(value)))
                throw new InvalidOperationException("Unexpected privilege in ACL snapshot.");
            return string.Join(", ", values);
        }

        await using var connection = new NpgsqlConnection(adminConnection);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        async Task Sql(string statement)
        {
            await using var command = new NpgsqlCommand(statement, connection, transaction);
            await command.ExecuteNonQueryAsync();
        }

        await Sql("REVOKE ALL ON ALL TABLES IN SCHEMA smartmart FROM smartmart_backend");
        foreach (var table in root.GetProperty("tables").EnumerateArray())
        {
            var name = "smartmart." + Identifier(table.GetProperty("name").GetString()!);
            await Sql($"DROP POLICY IF EXISTS backend_access ON {name}");
            await Sql($"DROP POLICY IF EXISTS smartmart_backend_trusted_server ON {name}");
            await Sql($"""
                CREATE POLICY smartmart_backend_trusted_server ON {name}
                FOR ALL TO smartmart_backend
                USING (CURRENT_USER = 'smartmart_backend'::name)
                WITH CHECK (CURRENT_USER = 'smartmart_backend'::name)
                """);
            await Sql($"GRANT {Permissions(table, new(["SELECT","INSERT","UPDATE","DELETE"]))} ON TABLE {name} TO smartmart_backend");
        }
        foreach (var view in root.GetProperty("views").EnumerateArray())
        {
            var name = "smartmart." + Identifier(view.GetProperty("name").GetString()!);
            await Sql($"GRANT {Permissions(view, new(["SELECT"]))} ON TABLE {name} TO smartmart_backend");
        }
        await Sql("REVOKE ALL ON ALL SEQUENCES IN SCHEMA smartmart FROM smartmart_backend");
        foreach (var sequence in root.GetProperty("sequences").EnumerateArray())
        {
            var name = "smartmart." + Identifier(sequence.GetProperty("name").GetString()!);
            await Sql($"GRANT {Permissions(sequence, new(["USAGE","SELECT"]))} ON SEQUENCE {name} TO smartmart_backend");
        }
        // Baseline functions have PostgreSQL's default PUBLIC EXECUTE. Reproduce the
        // observed owner-only function ACL in this disposable database, never in production.
        foreach (var function in root.GetProperty("functions").EnumerateArray())
        {
            if (function.GetProperty("backend_execute").GetBoolean())
                throw new InvalidOperationException("Unexpected executable function in observed snapshot.");
            var name = "smartmart." + Identifier(function.GetProperty("name").GetString()!) + "()";
            await Sql($"REVOKE EXECUTE ON FUNCTION {name} FROM PUBLIC, smartmart_backend");
        }
        await transaction.CommitAsync();
    }

    private static async Task Execute(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    public async Task DisposeAsync()
    {
        if (_server is null) return;
        await using var server = new NpgsqlConnection(_server);
        await server.OpenAsync();
        var errors = new List<Exception>();
        try { await DropObservedDatabaseAsync(); }
        catch (Exception ex) { errors.Add(ex); }
        if (_databaseCreated)
        {
            try
            {
                await Execute(server, $"DROP DATABASE IF EXISTS {_database} WITH (FORCE)");
                _databaseCreated = false;
            }
            catch (Exception ex) { errors.Add(ex); }
        }
        if (_roleCreated)
        {
            try
            {
                await Execute(server, $"DROP ROLE IF EXISTS {Role}");
                _roleCreated = false;
            }
            catch (Exception ex) { errors.Add(ex); }
        }
        if (errors.Count != 0) throw new AggregateException("Role fixture cleanup failed.", errors);
    }
}

// Cluster-wide smartmart_backend and fresh-cluster preconditions require one shared fixture.
[CollectionDefinition("BackendRoleContract", DisableParallelization = true)]
public sealed class BackendRoleContractCollection : ICollectionFixture<SmokeRolePostgreSqlFixture> { }

[Collection("BackendRoleContract")]
[Trait("Database", "PostgreSql")]
public sealed class SupabaseRuntimeRoleIntegrationTests(SmokeRolePostgreSqlFixture fixture)
{
    private static async Task<(int Exit, JsonElement Report, string Output)> Run(string connection)
    {
        var directory = Path.Combine(Path.GetTempPath(), "smartmart-role-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "result.json");
            using var log = new StringWriter();
            var exit = await SmokeRunner.RunAsync(connection, path, log, log);
            var json = await File.ReadAllTextAsync(path);
            using var report = JsonDocument.Parse(json);
            var output = log.ToString() + json;
            Assert.DoesNotContain(new NpgsqlConnectionStringBuilder(connection).Password!, output);
            return (exit, report.RootElement.Clone(), output);
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Fact]
    public async Task ActualBackendLogin_ProducesVerifiedReadOnlyArtifactForAll34Objects()
    {
        var result = await Run(fixture.BackendConnection);
        Assert.Equal("passed", result.Report.GetProperty("status").GetString());
        Assert.Equal(0, result.Exit);
        Assert.True(result.Report.GetProperty("roleVerified").GetBoolean());
        Assert.Equal("smartmart_backend", result.Report.GetProperty("currentUser").GetString());
        Assert.Equal("smartmart_backend", result.Report.GetProperty("sessionUser").GetString());
        foreach (var property in new[] { "isSuperuser", "bypassRls", "canCreateRole", "canCreateDatabase" })
            Assert.False(result.Report.GetProperty(property).GetBoolean());
        Assert.True(result.Report.GetProperty("rowSecurity").GetBoolean());
        Assert.True(result.Report.GetProperty("readOnly").GetBoolean());
        Assert.Equal(31, result.Report.GetProperty("tables").GetInt32());
        Assert.Equal(3, result.Report.GetProperty("views").GetInt32());
        Assert.Equal(34, result.Report.GetProperty("checkedObjects").GetInt32());
        Assert.Equal(0, result.Report.GetProperty("writes").GetInt32());
        var probes = result.Report.GetProperty("probes").EnumerateArray().ToArray();
        Assert.Equal(34, probes.Select(p => p.GetProperty("name").GetString()).Distinct().Count());
        Assert.Equal(1, Assert.Single(probes, p => p.GetProperty("name").GetString() == "smartmart.vai_tro")
            .GetProperty("rowsRead").GetInt32());
        // The production transaction has been rolled back and the connection disposed.
        await using var verify = new NpgsqlConnection(fixture.AdminConnection);
        await verify.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT count(*) FROM smartmart.vai_tro", verify);
        Assert.Equal(1L, (long)(await command.ExecuteScalarAsync())!);
    }

    [Fact]
    public async Task ActualAdminLogin_IsRejectedBeforeMappedQueries()
    {
        var result = await Run(fixture.AdminConnection);
        AssertRejected(result.Exit, result.Report, "UnexpectedDatabaseRole");
        Assert.Equal(JsonValueKind.Null, result.Report.GetProperty("currentUser").ValueKind);
        Assert.Equal(JsonValueKind.Null, result.Report.GetProperty("sessionUser").ValueKind);
    }

    [Theory]
    [InlineData("super", "SuperuserNotAllowed", "isSuperuser", true)]
    [InlineData("bypass", "BypassRlsNotAllowed", "bypassRls", true)]
    [InlineData("create-role", "CreateRoleNotAllowed", "canCreateRole", true)]
    [InlineData("create-db", "CreateDatabaseNotAllowed", "canCreateDatabase", true)]
    [InlineData("row-security", "RowSecurityNotEnabled", "rowSecurity", false)]
    public async Task ActualUnsafeBackendRole_IsRejected(string state, string reason, string property, bool expected)
    {
        try
        {
            await fixture.AssertSafeRoleAsync();
            await fixture.SetRoleStateAsync(state);
            var result = await Run(fixture.BackendConnection);
            AssertRejected(result.Exit, result.Report, reason);
            Assert.Equal(expected, result.Report.GetProperty(property).GetBoolean());
        }
        finally
        {
            await fixture.SetRoleStateAsync("reset");
            await fixture.AssertSafeRoleAsync();
        }
    }

    [Fact]
    public async Task ActualSetRole_CannotHideDifferentSessionUser()
    {
        await using var connection = new NpgsqlConnection(fixture.AdminConnection);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using (var command = new NpgsqlCommand(
            "SET TRANSACTION READ ONLY; SET LOCAL ROLE smartmart_backend", connection, transaction))
            await command.ExecuteNonQueryAsync();
        var evidence = await PostgreSqlSmokeSession.ReadRoleEvidenceAsync(connection, transaction);
        Assert.Equal("smartmart_backend", evidence.CurrentUser);
        Assert.Equal("postgres", evidence.SessionUser);
        var report = new SmokeReport();
        var failure = Assert.Throws<SmokeValidationException>(() => SmokeRoleGuard.Verify(evidence, report));
        Assert.Equal("UnexpectedSessionRole", failure.Code);
        Assert.False(report.RoleVerified);
        Assert.Null(report.SessionUser);
        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task ActualReadWriteTransaction_IsRejectedByRoleGuard()
    {
        await using var connection = new NpgsqlConnection(fixture.BackendConnection);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using (var command = new NpgsqlCommand("SET TRANSACTION READ WRITE", connection, transaction))
            await command.ExecuteNonQueryAsync();
        var evidence = await PostgreSqlSmokeSession.ReadRoleEvidenceAsync(connection, transaction);
        Assert.False(evidence.ReadOnly);
        var report = new SmokeReport();
        var failure = Assert.Throws<SmokeValidationException>(() => SmokeRoleGuard.Verify(evidence, report));
        Assert.Equal("ReadOnlyGuardNotApplied", failure.Code);
        Assert.False(report.RoleVerified);
        await transaction.RollbackAsync();
    }

    private static void AssertRejected(int exit, JsonElement report, string reason)
    {
        Assert.Equal(1, exit);
        Assert.Equal("failed", report.GetProperty("status").GetString());
        Assert.Equal(reason, report.GetProperty("reasonCode").GetString());
        Assert.False(report.GetProperty("roleVerified").GetBoolean());
        Assert.Equal(0, report.GetProperty("checkedObjects").GetInt32());
        Assert.Equal(0, report.GetProperty("writes").GetInt32());
        Assert.Equal(JsonValueKind.Null, report.GetProperty("failedObject").ValueKind);
    }
}
