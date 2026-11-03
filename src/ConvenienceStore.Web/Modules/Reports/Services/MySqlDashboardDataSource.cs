using System.Data.Common;
using System.Globalization;
using ConvenienceStore.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Modules.Reports.Services;

/// <summary>Dashboard queries mapped to the MySQL tables and column names in the shared schema.</summary>
public sealed class MySqlDashboardDataSource(CuaHangDbContext db) : IDashboardDataSource
{
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);
    private readonly CuaHangDbContext _db = db;

    public async Task<IReadOnlyList<PaidInvoiceFact>> GetPaidInvoicesAsync(
        DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT h.id invoice_id, h.thanh_toan_luc paid_at, l.san_pham_id product_id,
                   COALESCE(l.ten_san_pham, p.ten_san_pham, '') product_name,
                   l.so_luong quantity, l.don_gia list_price, l.giam_don_vi discount_per_unit
            FROM hoa_don h
            JOIN ct_hoa_don l ON l.hoa_don_id = h.id
            LEFT JOIN san_pham p ON p.id = l.san_pham_id
            WHERE h.trang_thai = 'DA_THANH_TOAN' AND h.thanh_toan_luc >= @from AND h.thanh_toan_luc < @to
            ORDER BY h.id, l.san_pham_id
            """;
        var invoiceLines = await QueryAsync(sql, from, to, r =>
            (Id: Long(r, "invoice_id"), PaidAt: VietnamDate(r, "paid_at"),
             Line: new InvoiceLineFact(Long(r, "product_id"), Text(r, "product_name"),
                 Int(r, "quantity"), Long(r, "list_price")), Discount: Long(r, "discount_per_unit")), cancellationToken);

        var invoices = invoiceLines.GroupBy(x => x.Id).Select(g => new PaidInvoiceFact(
            g.Key, g.First().PaidAt, g.Select(x => x.Line).ToArray(),
            g.Sum(x => (long)x.Line.Quantity * x.Discount), 0)).ToList();

        // Refunds are dated on return completion, not retroactively on the original sale date.
        var returns = await QueryAsync("""
            SELECT h.id return_id, h.hoan_tat_luc returned_at, h.tien_hoan refund_amount
            FROM phieu_doi_tra h
            WHERE h.trang_thai = 'HOAN_TAT' AND h.hoan_tat_luc >= @from AND h.hoan_tat_luc < @to
            ORDER BY h.id
            """, from, to, r => new PaidInvoiceFact(-Long(r, "return_id"), VietnamDate(r, "returned_at"),
                [], 0, Long(r, "refund_amount"), IsReturn: true), cancellationToken);
        invoices.AddRange(returns);
        return invoices;
    }

    public Task<int> GetLowStockCountAsync(CancellationToken cancellationToken) => QueryScalarAsync("""
        SELECT COUNT(*) FROM ton_kho WHERE so_luong_ton <= nguong_toi_thieu
        """, cancellationToken);

    public async Task<(int Stocktake, int WriteOff, int Returns)> GetPendingApprovalCountsAsync(
        CancellationToken cancellationToken)
    {
        var counts = await QueryScalarRowsAsync("""
            SELECT (SELECT COUNT(*) FROM phieu_kiem_ke WHERE trang_thai = 'CHO_DUYET') stocktake_count,
                   (SELECT COUNT(*) FROM phieu_xuat_huy WHERE trang_thai = 'CHO_DUYET') writeoff_count,
                   (SELECT COUNT(*) FROM phieu_doi_tra WHERE trang_thai = 'CHO_DUYET') return_count
            """, cancellationToken);
        return (counts[0], counts[1], counts[2]);
    }

    private async Task<IReadOnlyList<T>> QueryAsync<T>(string sql, DateTimeOffset from, DateTimeOffset to,
        Func<DbDataReader,T> map, CancellationToken ct)
    {
        var connection = _db.Database.GetDbConnection();
        var close = connection.State != System.Data.ConnectionState.Open;
        if (close) await connection.OpenAsync(ct);
        try
        {
            await using var command = connection.CreateCommand(); command.CommandText = sql;
            Param(command, "@from", DbDate(from)); Param(command, "@to", DbDate(to));
            await using var reader = await command.ExecuteReaderAsync(ct);
            var result = new List<T>(); while (await reader.ReadAsync(ct)) result.Add(map(reader));
            return result;
        }
        finally { if (close) await connection.CloseAsync(); }
    }

    private async Task<int> QueryScalarAsync(string sql, CancellationToken ct)
    {
        var values = await QueryScalarRowsAsync(sql, ct); return values[0];
    }

    private async Task<int[]> QueryScalarRowsAsync(string sql, CancellationToken ct)
    {
        var connection = _db.Database.GetDbConnection(); var close = connection.State != System.Data.ConnectionState.Open;
        if (close) await connection.OpenAsync(ct);
        try
        {
            await using var command = connection.CreateCommand(); command.CommandText = sql;
            await using var reader = await command.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct)) return [0, 0, 0];
            return Enumerable.Range(0, reader.FieldCount).Select(i => Convert.ToInt32(reader.GetValue(i), CultureInfo.InvariantCulture)).ToArray();
        }
        finally { if (close) await connection.CloseAsync(); }
    }

    private static void Param(DbCommand c, string name, object value) { var p=c.CreateParameter(); p.ParameterName=name; p.Value=value; c.Parameters.Add(p); }
    private static DateTime DbDate(DateTimeOffset d) => DateTime.SpecifyKind(d.ToOffset(VietnamOffset).DateTime, DateTimeKind.Unspecified);
    private static DateTimeOffset VietnamDate(DbDataReader r,string n) => new(DateTime.SpecifyKind(Convert.ToDateTime(r[n],CultureInfo.InvariantCulture),DateTimeKind.Unspecified),VietnamOffset);
    private static long Long(DbDataReader r,string n) => r[n] is DBNull ? 0 : Convert.ToInt64(r[n],CultureInfo.InvariantCulture);
    private static int Int(DbDataReader r,string n) => r[n] is DBNull ? 0 : Convert.ToInt32(r[n],CultureInfo.InvariantCulture);
    private static string Text(DbDataReader r,string n) => r[n] is DBNull ? "" : Convert.ToString(r[n],CultureInfo.InvariantCulture) ?? "";
}
