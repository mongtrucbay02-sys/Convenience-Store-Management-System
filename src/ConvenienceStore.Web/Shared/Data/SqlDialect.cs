using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ConvenienceStore.Web.Shared.Data;

public static class SqlDialect
{
    public static string CounterUpsert(this DbContext db) =>
        "ON CONFLICT (tien_to,ky) DO UPDATE SET so_cuoi=bo_dem_chung_tu.so_cuoi+1";

    public static DbConnection CreateConnection(IConfiguration configuration) =>
        new NpgsqlConnection(PostgresConnectionString(configuration.GetConnectionString("CuaHang")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:CuaHang.")));

    public static string PostgresConnectionString(string value) => new NpgsqlConnectionStringBuilder(value)
    { SearchPath = "smartmart", Timezone = "Asia/Ho_Chi_Minh" }.ConnectionString;

    public static DbCommand Command(string sql, DbConnection connection, DbTransaction? transaction = null)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = transaction;
        return command;
    }

    public static void AddValue(this DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = Normalize(command.Connection!, value) ?? DBNull.Value;
        // PostgreSQL cannot infer the type of a parameter used first in IS NULL.
        if (value is null or DBNull && command.Connection is NpgsqlConnection)
        {
            if (name is "@term" or "@keyword" or "@ma" or "@khach") parameter.DbType = DbType.String;
            if (name is "@categoryId" or "@id" or "@invoice" or "@return" or "@kh" or "@promotion") parameter.DbType = DbType.Int64;
            if (name is "@from" or "@to" or "@start" or "@end") parameter.DbType = DbType.DateTime;
        }
        command.Parameters.Add(parameter);
    }

    public static object? Normalize(DbConnection connection, object? value) => value switch
    {
        DateTime date when date.Kind == DateTimeKind.Unspecified => new DateTimeOffset(date, TimeSpan.FromHours(7)).UtcDateTime,
        DateTime date => date.ToUniversalTime(),
        DateTimeOffset date => date.UtcDateTime,
        uint number => (long)number,
        ulong number => checked((long)number),
        _ => value
    };

    public static bool IsUniqueViolation(Exception exception) => exception switch
    {
        PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } => true,
        _ => false
    };

    public static bool IsForeignKeyViolation(Exception exception) => exception switch
    {
        PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation } => true,
        _ => false
    };

    public static async Task<long> InsertIdAsync(this DbContext db, string insert, CancellationToken ct,
        params (string Ten, object? GiaTri)[] values)
    {
        var sql = insert.Trim().TrimEnd(';') + " RETURNING id;";
        return Convert.ToInt64(await db.DocMotGiaTriAsync(sql, ct, values));
    }

    public static async Task<long> InsertIdAsync(DbConnection connection, DbTransaction transaction, string insert,
        CancellationToken ct, IReadOnlyDictionary<string, object?> values)
    {
        await using var command = Command(insert.Trim().TrimEnd(';') + " RETURNING id;", connection, transaction);
        foreach (var (name, value) in values) command.AddValue(name, value);
        return Convert.ToInt64(await command.ExecuteScalarAsync(ct));
    }
}
