using System.Text.Json;
using Npgsql;

namespace ConvenienceStore.Web.Tests.PostgreSql;

// The exact B02 AS-IS grants are replayed on a dedicated disposable PG17 cluster,
// and all business SQL uses a real smartmart_backend login (not SET ROLE / postgres).
[Collection("BackendRoleContract")]
[Trait("Database", "PostgreSql")]
public sealed class ObservedAclPostgreSqlTests(SmokeRolePostgreSqlFixture fixture)
{
    [Fact]
    public async Task RealBackendLogin_ObservedAclAndRls_DenyForbiddenWritesAndRollbackAllowedWrite()
    {
        var baseline = await fixture.ReadBaselineAclFingerprintAsync();
        await fixture.PrepareObservedDatabaseAsync();
        try
        {
            Assert.Equal(baseline, await fixture.ReadBaselineAclFingerprintAsync());
            Assert.NotEqual(new NpgsqlConnectionStringBuilder(fixture.BackendConnection).Database,
                new NpgsqlConnectionStringBuilder(fixture.ObservedBackendConnection).Database);
            await VerifyObservedAclAsync();
        }
        finally
        {
            await fixture.DropObservedDatabaseAsync();
            await fixture.AssertSafeRoleAsync();
            Assert.Equal(baseline, await fixture.ReadBaselineAclFingerprintAsync());
        }
    }

    [Fact]
    public async Task RealBackendLogin_ObservedCatalog_MatchesExactLocalSecurityContract()
    {
        var baseline = await fixture.ReadBaselineAclFingerprintAsync();
        await fixture.PrepareObservedDatabaseAsync();
        try
        {
            await VerifyCatalogAsync();
        }
        finally
        {
            await fixture.DropObservedDatabaseAsync();
            await fixture.AssertSafeRoleAsync();
            Assert.Equal(baseline, await fixture.ReadBaselineAclFingerprintAsync());
        }
    }

    // Local AS-IS profile only: Supabase-managed roles, incoming platform memberships,
    // public event triggers and live default ACLs are not emulated or certified here.
    private async Task VerifyCatalogAsync()
    {
        using var snapshot = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(
            AppContext.BaseDirectory, "SchemaReference", "observed-security-contract.json")));
        var root = snapshot.RootElement;
        await using var connection = new NpgsqlConnection(fixture.ObservedBackendConnection);
        await connection.OpenAsync();

        async Task<T> Scalar<T>(string sql, params NpgsqlParameter[] parameters)
        {
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddRange(parameters);
            var value = await command.ExecuteScalarAsync();
            Assert.True(value is T, "Unexpected catalog result: " + sql);
            return (T)value!;
        }

        static string[] Strings(JsonElement values) => values.EnumerateArray()
            .Select(v => v.GetString()!).OrderBy(v => v, StringComparer.Ordinal).ToArray();
        static string[] Sorted(IEnumerable<string> values) =>
            values.OrderBy(v => v, StringComparer.Ordinal).ToArray();
        static void EqualSet(IEnumerable<string> expected, IEnumerable<string> actual, string context) =>
            Assert.True(Sorted(expected).SequenceEqual(Sorted(actual)), context + ": expected [" +
                string.Join(", ", Sorted(expected)) + "], actual [" + string.Join(", ", Sorted(actual)) + "]");
        static NpgsqlParameter P(string name, string value) => new(name, value);

        Assert.True(await Scalar<bool>("""
            SELECT current_user = 'smartmart_backend' AND session_user = current_user
               AND current_setting('row_security') = 'on'
               AND rolcanlogin AND NOT rolsuper AND NOT rolbypassrls
               AND NOT rolcreaterole AND NOT rolcreatedb AND NOT rolreplication AND NOT rolinherit
               AND rolconnlimit = -1 AND rolvaliduntil IS NULL
               AND coalesce(cardinality(rolconfig),0) = 0
            FROM pg_roles WHERE rolname = current_user
            """));
        Assert.Equal(0L, await Scalar<long>("""
            SELECT count(*) FROM pg_auth_members m JOIN pg_roles r ON r.oid=m.member
            WHERE r.rolname=current_user
            """));
        Assert.Equal(0L, await Scalar<long>("""
            SELECT count(*) FROM pg_db_role_setting s JOIN pg_roles r ON r.oid=s.setrole
            WHERE r.rolname=current_user
            """));

        var schema = root.GetProperty("schema");
        Assert.Equal(schema.GetProperty("owner").GetString(), await Scalar<string>("""
            SELECT pg_get_userbyid(nspowner) FROM pg_namespace WHERE nspname='smartmart'
            """));
        EqualSet(Strings(schema.GetProperty("acl")), await Scalar<string[]>("""
            SELECT ARRAY(SELECT x::text FROM unnest(nspacl) x)
            FROM pg_namespace WHERE nspname='smartmart'
            """), "schema ACL");
        foreach (var privilege in new[] { "USAGE", "CREATE", "USAGE WITH GRANT OPTION", "CREATE WITH GRANT OPTION" })
            Assert.Equal(privilege == "USAGE", await Scalar<bool>(
                "SELECT has_schema_privilege(current_user, 'smartmart', @privilege)", P("privilege", privilege)));

        var dbPrivileges = root.GetProperty("previous_catalog_checks").GetProperty("database_privileges");
        foreach (var privilege in new[] { "CONNECT", "CREATE", "TEMP" })
            Assert.Equal(dbPrivileges.GetProperty(privilege).GetBoolean(), await Scalar<bool>(
                "SELECT has_database_privilege(current_user, current_database(), @privilege)", P("privilege", privilege)));

        var tableRights = new[] { "SELECT", "INSERT", "UPDATE", "DELETE", "TRUNCATE", "REFERENCES", "TRIGGER", "MAINTAIN" };
        foreach (var group in new[] { (Key: "tables", Kinds: "r,p"), (Key: "views", Kinds: "v"), (Key: "sequences", Kinds: "S") })
        {
            var entries = root.GetProperty(group.Key).EnumerateArray().ToArray();
            EqualSet(entries.Select(e => e.GetProperty("name").GetString()!), await Scalar<string[]>("""
                SELECT ARRAY(SELECT c.relname::text FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
                    WHERE n.nspname='smartmart' AND c.relkind::text=ANY(string_to_array(@kinds,',')))
                """, P("kinds", group.Kinds)), group.Key + " object inventory");

            foreach (var entry in entries)
            {
                var name = "smartmart." + entry.GetProperty("name").GetString()!;
                var expected = Strings(entry.GetProperty("backend"));
                Assert.Equal(entry.GetProperty("owner").GetString(), await Scalar<string>(
                    "SELECT pg_get_userbyid(relowner) FROM pg_class WHERE oid=to_regclass(@name)", P("name", name)));
                EqualSet(Strings(entry.GetProperty("acl")), await Scalar<string[]>("""
                    SELECT ARRAY(SELECT x::text FROM unnest(relacl) x) FROM pg_class WHERE oid=to_regclass(@name)
                    """, P("name", name)), name + " direct ACL");
                var rights = group.Key == "sequences" ? new[] { "USAGE", "SELECT", "UPDATE" } : tableRights;
                var sql = group.Key == "sequences"
                    ? "SELECT has_sequence_privilege(current_user, @name, @privilege)"
                    : "SELECT has_table_privilege(current_user, @name, @privilege)";
                foreach (var right in rights)
                {
                    Assert.True(expected.Contains(right) == await Scalar<bool>(sql, P("name", name), P("privilege", right)),
                        name + " effective " + right);
                    Assert.False(await Scalar<bool>(sql, P("name", name), P("privilege", right + " WITH GRANT OPTION")),
                        name + " grant option " + right);
                }
                if (group.Key == "tables")
                {
                    Assert.Equal(entry.GetProperty("rls").GetBoolean(), await Scalar<bool>(
                        "SELECT relrowsecurity FROM pg_class WHERE oid=to_regclass(@name)", P("name", name)));
                    Assert.Equal(entry.GetProperty("force_rls").GetBoolean(), await Scalar<bool>(
                        "SELECT relforcerowsecurity FROM pg_class WHERE oid=to_regclass(@name)", P("name", name)));
                    Assert.True(await Scalar<bool>("SELECT row_security_active(to_regclass(@name))", P("name", name)), name);
                }
                if (group.Key == "views")
                    EqualSet(Strings(entry.GetProperty("options")), await Scalar<string[]>("""
                        SELECT coalesce(reloptions, ARRAY[]::text[]) FROM pg_class WHERE oid=to_regclass(@name)
                        """, P("name", name)), name + " view options");
            }
        }
        // Materialized views and foreign tables are outside the approved inventory.
        Assert.Equal(0L, await Scalar<long>("""
            SELECT count(*) FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
            WHERE n.nspname='smartmart' AND c.relkind IN ('m','f')
            """));
        Assert.Equal(0L, await Scalar<long>("""
            SELECT count(*) FROM pg_attribute a JOIN pg_class c ON c.oid=a.attrelid
            JOIN pg_namespace n ON n.oid=c.relnamespace
            WHERE n.nspname='smartmart' AND a.attnum>0 AND NOT a.attisdropped AND a.attacl IS NOT NULL
            """));
        // Check explicit local defaults for the deployment owner; do not infer live platform defaults.
        Assert.Equal(0L, await Scalar<long>("""
            SELECT count(*) FROM pg_default_acl d JOIN pg_roles r ON r.oid=d.defaclrole
            WHERE r.rolname='postgres' AND (d.defaclnamespace=0 OR d.defaclnamespace=
                (SELECT oid FROM pg_namespace WHERE nspname='smartmart'))
            """));

        var tables = root.GetProperty("tables").EnumerateArray().ToArray();
        Assert.Equal((long)tables.Length, await Scalar<long>(
            "SELECT count(*) FROM pg_policies WHERE schemaname='smartmart'"));
        foreach (var table in tables)
        {
            var name = table.GetProperty("name").GetString()!;
            var policy = table.GetProperty("policy");
            // pg_policies renders the parsed expression; compare to the recorded PG17 expression.
            var actual = await Scalar<string>("""
                SELECT jsonb_build_object('name',policyname,'permissive',permissive,'roles',roles,
                    'cmd',cmd,'qual',qual,'with_check',with_check)::text
                FROM pg_policies WHERE schemaname='smartmart' AND tablename=@name
                """, P("name", name));
            using var parsed = JsonDocument.Parse(actual);
            foreach (var key in new[] { "name", "permissive", "cmd", "qual", "with_check" })
                Assert.Equal(policy.GetProperty(key).GetString(), parsed.RootElement.GetProperty(key).GetString());
            EqualSet(Strings(policy.GetProperty("roles")), Strings(parsed.RootElement.GetProperty("roles")), name + " policy roles");
        }

        var functions = root.GetProperty("functions").EnumerateArray().ToArray();
        EqualSet(functions.Select(e => e.GetProperty("name").GetString()! + "()"), await Scalar<string[]>("""
            SELECT ARRAY(SELECT p.proname::text || '(' || pg_get_function_identity_arguments(p.oid) || ')'
                FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace WHERE n.nspname='smartmart')
            """), "function inventory");
        foreach (var function in functions)
        {
            var name = "smartmart." + function.GetProperty("name").GetString()! + "()";
            var actual = await Scalar<string>("""
                SELECT jsonb_build_object('owner',pg_get_userbyid(proowner),'security_definer',prosecdef,
                    'config',proconfig,'returns',pg_get_function_result(oid))::text
                FROM pg_proc WHERE oid=to_regprocedure(@name)
                """, P("name", name));
            using var parsed = JsonDocument.Parse(actual);
            foreach (var key in new[] { "owner", "returns" })
                Assert.Equal(function.GetProperty(key).GetString(), parsed.RootElement.GetProperty(key).GetString());
            Assert.Equal(function.GetProperty("security_definer").GetBoolean(), parsed.RootElement.GetProperty("security_definer").GetBoolean());
            EqualSet(Strings(function.GetProperty("config")), Strings(parsed.RootElement.GetProperty("config")), name + " config");
            EqualSet(Strings(function.GetProperty("acl")), await Scalar<string[]>("""
                SELECT ARRAY(SELECT x::text FROM unnest(proacl) x) FROM pg_proc WHERE oid=to_regprocedure(@name)
                """, P("name", name)), name + " function ACL");
            Assert.Equal(function.GetProperty("backend_execute").GetBoolean(), await Scalar<bool>(
                "SELECT has_function_privilege(current_user, @name, 'EXECUTE')", P("name", name)));
            Assert.False(await Scalar<bool>(
                "SELECT has_function_privilege(current_user, @name, 'EXECUTE WITH GRANT OPTION')", P("name", name)));
        }
    }

    private async Task VerifyObservedAclAsync()
    {
        var snapshotPath = Path.Combine(AppContext.BaseDirectory,
            "SchemaReference", "observed-security-contract.json");
        using var snapshot = JsonDocument.Parse(await File.ReadAllTextAsync(snapshotPath));
        var entries = snapshot.RootElement.GetProperty("tables").EnumerateArray().ToArray();

        await using var connection = new NpgsqlConnection(fixture.ObservedBackendConnection);
        await connection.OpenAsync();

        async Task<object?> Scalar(string sql, params NpgsqlParameter[] parameters)
        {
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddRange(parameters);
            return await command.ExecuteScalarAsync();
        }

        Assert.Equal("smartmart_backend", (string)(await Scalar("SELECT current_user"))!);
        Assert.Equal("smartmart_backend", (string)(await Scalar("SELECT session_user"))!);
        Assert.Equal("on", (string)(await Scalar("SHOW row_security"))!);
        Assert.Equal(31, entries.Length);

        var known = new HashSet<string> { "SELECT", "INSERT", "UPDATE", "DELETE" };
        foreach (var table in entries)
        {
            var name = "smartmart." + table.GetProperty("name").GetString()!;
            var expected = table.GetProperty("backend").EnumerateArray()
                .Select(p => p.GetString()!).ToHashSet();
            foreach (var operation in known)
            {
                var allowed = (bool)(await Scalar(
                    "SELECT has_table_privilege(current_user, @table, @privilege)",
                    new NpgsqlParameter("table", name),
                    new NpgsqlParameter("privilege", operation)))!;
                Assert.Equal(expected.Contains(operation), allowed);
            }
            Assert.False((bool)(await Scalar(
                "SELECT has_table_privilege(current_user, @table, 'TRUNCATE')",
                new NpgsqlParameter("table", name)))!);
        }

        await using (var transaction = await connection.BeginTransactionAsync())
        {
            await using var insert = new NpgsqlCommand(
                "INSERT INTO smartmart.danh_muc (ten) VALUES ('QA ACL disposable') RETURNING id",
                connection, transaction);
            var id = (long)(await insert.ExecuteScalarAsync())!;
            Assert.True(id > 0);
            await using var count = new NpgsqlCommand(
                "SELECT COUNT(*) FROM smartmart.danh_muc WHERE id=@id", connection, transaction);
            count.Parameters.AddWithValue("id", id);
            Assert.Equal(1L, (long)(await count.ExecuteScalarAsync())!);
            await transaction.RollbackAsync();
            Assert.Equal(0L, (long)(await Scalar(
                "SELECT COUNT(*) FROM smartmart.danh_muc WHERE id=@id",
                new NpgsqlParameter("id", id)))!);
        }

        await using (var denied = new NpgsqlCommand(
            "DELETE FROM smartmart.vai_tro WHERE id=1", connection))
        {
            var failure = await Assert.ThrowsAsync<PostgresException>(async () =>
                await denied.ExecuteNonQueryAsync());
            Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, failure.SqlState);
        }
        await using (var denied = new NpgsqlCommand(
            "UPDATE smartmart.ct_hoa_don SET so_luong=so_luong WHERE FALSE", connection))
        {
            var failure = await Assert.ThrowsAsync<PostgresException>(async () =>
                await denied.ExecuteNonQueryAsync());
            Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, failure.SqlState);
        }
        Assert.Equal(1L, (long)(await Scalar(
            "SELECT COUNT(*) FROM smartmart.vai_tro WHERE id=1"))!);
    }
}
