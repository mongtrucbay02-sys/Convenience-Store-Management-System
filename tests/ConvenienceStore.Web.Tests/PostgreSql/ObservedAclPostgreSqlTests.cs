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
