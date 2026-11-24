using System.Data.Common;
using ConvenienceStore.Web.Shared.Data;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ConvenienceStore.Web.Data;

public sealed class PostgreSqlTimeInterceptor : DbCommandInterceptor
{
    private static void Normalize(DbCommand command)
    {
        foreach (DbParameter parameter in command.Parameters)
            parameter.Value = SqlDialect.Normalize(command.Connection!, parameter.Value) ?? DBNull.Value;
    }
    public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData data, InterceptionResult<DbDataReader> result)
    { Normalize(command); return result; }
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData data,
        InterceptionResult<DbDataReader> result, CancellationToken ct = default)
    { Normalize(command); return ValueTask.FromResult(result); }
    public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData data, InterceptionResult<int> result)
    { Normalize(command); return result; }
    public override InterceptionResult<object> ScalarExecuting(DbCommand command, CommandEventData data, InterceptionResult<object> result)
    { Normalize(command); return result; }
    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData data,
        InterceptionResult<int> result, CancellationToken ct = default)
    { Normalize(command); return ValueTask.FromResult(result); }
    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData data,
        InterceptionResult<object> result, CancellationToken ct = default)
    { Normalize(command); return ValueTask.FromResult(result); }
}
