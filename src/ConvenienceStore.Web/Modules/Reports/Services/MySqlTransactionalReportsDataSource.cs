using System.Data.Common;
using System.Globalization;
using ConvenienceStore.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Modules.Reports.Services;

/// <summary>
/// Direct MySQL queries for BC-07/08 against the shared Vietnamese schema.
/// It deliberately uses the copied invoice/purchase/return costs rather than current prices.
/// </summary>
public sealed class MySqlTransactionalReportsDataSource(CuaHangDbContext db)
    : IOperationalReportDataSource, ITransactionalReportsDataSource
{
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);
    private readonly CuaHangDbContext _db = db;

    public Task<IReadOnlyList<ReportProductFact>> GetProductsAsync(CancellationToken cancellationToken) =>
        QueryUnboundedAsync("""
            SELECT p.id AS product_id, p.ten_san_pham AS product_name, p.danh_muc_id AS category_id,
                   COALESCE(c.ten, '') AS category_name, COALESCE(k.gia_von_binh_quan, 0) AS unit_cost,
                   COALESCE(k.nguong_toi_thieu, 0) AS reorder_point, COALESCE(k.so_luong_ton, 0) AS current_stock
            FROM san_pham p
            LEFT JOIN danh_muc c ON c.id = p.danh_muc_id
            LEFT JOIN ton_kho k ON k.san_pham_id = p.id
            ORDER BY p.id
            """, reader => new ReportProductFact(Int64(reader, "product_id"), Text(reader, "product_name"),
            Int32(reader, "category_id"), Text(reader, "category_name"), RoundedInt64(reader, "unit_cost"),
            Int32(reader, "reorder_point"), Int32(reader, "current_stock")), cancellationToken);

    public Task<IReadOnlyList<ReportCancelledOrderFact>> GetCancelledOrdersAsync(
        DateTimeOffset fromInclusive, DateTimeOffset toExclusive, CancellationToken cancellationToken) =>
        QueryAsync("""
            SELECT g.id AS order_id, g.huy_luc AS cancelled_at, g.thu_ngan_id AS cashier_id,
                   COALESCE(a.ho_ten, '') AS cashier_name
            FROM gio_hang g LEFT JOIN tai_khoan a ON a.id = g.thu_ngan_id
            WHERE g.trang_thai = 'DA_HUY' AND g.huy_luc >= @fromInclusive AND g.huy_luc < @toExclusive
            ORDER BY g.huy_luc
            """, fromInclusive, toExclusive, reader => new ReportCancelledOrderFact(
            Int64(reader, "order_id"), VietnamDateTimeOffset(reader, "cancelled_at"),
            Int64(reader, "cashier_id"), Text(reader, "cashier_name")), cancellationToken);

    public Task<IReadOnlyList<ReportVoidedInvoiceFact>> GetVoidedInvoicesAsync(
        DateTimeOffset fromInclusive, DateTimeOffset toExclusive, CancellationToken cancellationToken) =>
        QueryAsync("""
            SELECT h.id AS invoice_id, h.huy_luc AS voided_at, h.thu_ngan_id AS cashier_id,
                   COALESCE(a.ho_ten, '') AS cashier_name
            FROM hoa_don h LEFT JOIN tai_khoan a ON a.id = h.thu_ngan_id
            WHERE h.trang_thai = 'DA_HUY' AND h.huy_luc >= @fromInclusive AND h.huy_luc < @toExclusive
            ORDER BY h.huy_luc
            """, fromInclusive, toExclusive, reader => new ReportVoidedInvoiceFact(
            Int64(reader, "invoice_id"), VietnamDateTimeOffset(reader, "voided_at"),
            Int64(reader, "cashier_id"), Text(reader, "cashier_name")), cancellationToken);

    public Task<IReadOnlyList<StockMovementFact>> GetStockMovementsAsync(
        DateTimeOffset fromInclusive, DateTimeOffset toExclusive, CancellationToken cancellationToken) =>
        QueryAsync("""
            SELECT san_pham_id AS product_id, thoi_diem AS occurred_at, so_luong_thay_doi AS quantity_change
            FROM the_kho
            WHERE thoi_diem >= @fromInclusive AND thoi_diem < @toExclusive
            ORDER BY thoi_diem, id
            """, fromInclusive, toExclusive, reader =>
            {
                var change = Int32(reader, "quantity_change");
                return new StockMovementFact(Int64(reader, "product_id"),
                    VietnamDateTimeOffset(reader, "occurred_at"), Math.Abs(change), change >= 0);
            }, cancellationToken);

    public async Task<IReadOnlyList<ReportPaidInvoiceFact>> GetPaidInvoicesAsync(
        DateTimeOffset fromInclusive, DateTimeOffset toExclusive, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT h.id AS invoice_id, h.thanh_toan_luc AS paid_at, h.thu_ngan_id AS cashier_id,
                   COALESCE(a.ho_ten, '') AS cashier_name,
                   GREATEST(COALESCE(h.tien_khach_dua,0) - COALESCE(h.tien_thua,0),0) AS cash_paid,
                   l.san_pham_id AS product_id, COALESCE(l.ten_san_pham, p.ten_san_pham, '') AS product_name,
                   COALESCE(p.danh_muc_id, 0) AS category_id, COALESCE(c.ten, '') AS category_name,
                   l.so_luong AS quantity, l.don_gia AS list_price, l.gia_von AS unit_cost,
                   l.giam_don_vi AS discount_per_unit
            FROM hoa_don h
            INNER JOIN ct_hoa_don l ON l.hoa_don_id = h.id
            LEFT JOIN tai_khoan a ON a.id = h.thu_ngan_id
            LEFT JOIN san_pham p ON p.id = l.san_pham_id
            LEFT JOIN danh_muc c ON c.id = p.danh_muc_id
            WHERE h.trang_thai = 'DA_THANH_TOAN'
              AND h.thanh_toan_luc >= @fromInclusive AND h.thanh_toan_luc < @toExclusive
            ORDER BY h.id, l.san_pham_id
            """;

        var invoices = await QueryAsync(sql, fromInclusive, toExclusive, reader =>
        {
            var id = Int64(reader, "invoice_id");
            var paidAt = VietnamDateTimeOffset(reader, "paid_at");
            var cashierId = Int64(reader, "cashier_id");
            var cashierName = Text(reader, "cashier_name");
            var cashPaid = Int64(reader, "cash_paid");
            var productId = Int64(reader, "product_id");
            var productName = Text(reader, "product_name");
            var categoryId = Int32(reader, "category_id");
            var categoryName = Text(reader, "category_name");
            var quantity = Int32(reader, "quantity");
            var listPrice = Int64(reader, "list_price");
            var unitCost = RoundedInt64(reader, "unit_cost");
            var discountPerUnit = Int64(reader, "discount_per_unit");
            return (id, paidAt, cashierId, cashierName, cashPaid,
                new ReportInvoiceLineFact(productId, productName, categoryId, categoryName,
                    quantity, listPrice, unitCost), discountPerUnit);
        }, cancellationToken);

        var grouped = new Dictionary<long, InvoiceBuilder>();
        foreach (var row in invoices)
        {
            if (!grouped.TryGetValue(row.id, out var invoice))
                grouped[row.id] = invoice = new InvoiceBuilder(row.id, row.paidAt, row.cashierId, row.cashierName, row.cashPaid);
            invoice.Lines.Add(row.Item6);
            invoice.PromotionDiscount += row.Item6.Quantity * row.discountPerUnit;
        }

        return grouped.Values.Select(x => new ReportPaidInvoiceFact(x.Id, x.PaidAt, x.CashierId, x.CashierName,
            x.Lines.ToArray(), x.PromotionDiscount, RefundAmount: 0, x.CashPaid)).ToArray();
    }

    public async Task<IReadOnlyList<ReportPurchaseReceiptFact>> GetPurchaseReceiptsAsync(
        DateTimeOffset fromInclusive, DateTimeOffset toExclusive, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT h.id AS receipt_id,
                   COALESCE(h.xac_nhan_luc, CAST(h.ngay_nhap AS DATETIME)) AS received_at,
                   h.nha_cung_cap_id AS supplier_id, COALESCE(n.ten, '') AS supplier_name,
                   l.san_pham_id AS product_id, COALESCE(l.ten_san_pham, '') AS product_name,
                   l.so_luong AS quantity, l.gia_nhap AS unit_cost
            FROM phieu_nhap h
            INNER JOIN ct_phieu_nhap l ON l.phieu_nhap_id = h.id
            LEFT JOIN nha_cung_cap n ON n.id = h.nha_cung_cap_id
            WHERE h.trang_thai = 'DA_NHAP_KHO'
              AND COALESCE(h.xac_nhan_luc, CAST(h.ngay_nhap AS DATETIME)) >= @fromInclusive
              AND COALESCE(h.xac_nhan_luc, CAST(h.ngay_nhap AS DATETIME)) < @toExclusive
            ORDER BY h.id, l.san_pham_id
            """;
        var rows = await QueryAsync(sql, fromInclusive, toExclusive, reader =>
            (Id: Int64(reader, "receipt_id"), ReceivedAt: VietnamDateTimeOffset(reader, "received_at"),
             SupplierId: Int32(reader, "supplier_id"), SupplierName: Text(reader, "supplier_name"),
             Line: new ReportPurchaseReceiptLineFact(Int64(reader, "product_id"), Text(reader, "product_name"),
                 Int32(reader, "quantity"), Int64(reader, "unit_cost"))), cancellationToken);
        return rows.GroupBy(x => x.Id).Select(group => new ReportPurchaseReceiptFact(
            group.Key, group.First().ReceivedAt, group.First().SupplierId, group.First().SupplierName,
            group.Select(x => x.Line).ToArray())).ToArray();
    }

    public async Task<IReadOnlyList<ReportReturnFact>> GetReturnsAsync(
        DateTimeOffset fromInclusive, DateTimeOffset toExclusive, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT h.id AS return_id, h.hoa_don_goc_id AS invoice_id, h.hoan_tat_luc AS returned_at,
                   h.thu_ngan_id AS cashier_id, COALESCE(a.ho_ten, '') AS cashier_name,
                   l.san_pham_id AS product_id, COALESCE(p.ten_san_pham, '') AS product_name,
                   COALESCE(p.danh_muc_id, 0) AS category_id, COALESCE(c.ten, '') AS category_name,
                   h.tien_hoan AS total_refund, l.so_luong AS quantity, l.gia_von AS unit_cost,
                   l.cach_xu_ly AS disposition, l.thanh_tien AS returned_value, l.gia_tri_huy AS destroyed_value
            FROM phieu_doi_tra h
            INNER JOIN ct_phieu_doi_tra l ON l.phieu_doi_tra_id = h.id
            LEFT JOIN tai_khoan a ON a.id = h.thu_ngan_id
            LEFT JOIN san_pham p ON p.id = l.san_pham_id
            LEFT JOIN danh_muc c ON c.id = p.danh_muc_id
            WHERE h.trang_thai = 'HOAN_TAT'
              AND h.hoan_tat_luc >= @fromInclusive AND h.hoan_tat_luc < @toExclusive
            ORDER BY h.id, l.san_pham_id
            """;
        var rows = await QueryAsync(sql, fromInclusive, toExclusive, reader =>
        {
            var quantity = Int32(reader, "quantity");
            var disposition = Text(reader, "disposition");
            var line = new ReportReturnedLineFact(Int64(reader, "product_id"), Text(reader, "product_name"),
                Int32(reader, "category_id"), Text(reader, "category_name"), quantity,
                RoundedInt64(reader, "unit_cost"), disposition == "NHAP_LAI" ? quantity : 0,
                Int64(reader, "returned_value"), RoundedInt64(reader, "destroyed_value"));
            return (Id: Int64(reader, "return_id"), InvoiceId: Int64(reader, "invoice_id"),
                ReturnedAt: VietnamDateTimeOffset(reader, "returned_at"), CashierId: Int64(reader, "cashier_id"),
                CashierName: Text(reader, "cashier_name"), TotalRefund: Int64(reader, "total_refund"), Line: line);
        }, cancellationToken);
        return rows.GroupBy(x => x.Id).Select(group =>
        {
            var lines = group.ToArray();
            var totalWeight = lines.Sum(x => x.Line.RefundAmount);
            var allocated = 0L;
            var allocatedLines = lines.Select((row, index) =>
            {
                var amount = index == lines.Length - 1
                    ? row.TotalRefund - allocated
                    : totalWeight <= 0 ? 0
                    : (long)Math.Floor((decimal)row.TotalRefund * row.Line.RefundAmount / totalWeight);
                allocated += amount;
                return row.Line with { RefundAmount = amount };
            }).ToArray();
            return new ReportReturnFact(group.Key, lines[0].InvoiceId, lines[0].ReturnedAt, allocatedLines,
                lines[0].CashierId, lines[0].CashierName);
        }).ToArray();
    }

    public async Task<IReadOnlyList<ReportWriteOffFact>> GetWriteOffsAsync(
        DateTimeOffset fromInclusive, DateTimeOffset toExclusive, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT h.id AS writeoff_id, h.duyet_luc AS written_off_at, l.san_pham_id AS product_id,
                   COALESCE(p.danh_muc_id, 0) AS category_id, l.so_luong AS quantity,
                   l.gia_von_luc_duyet AS unit_cost
            FROM phieu_xuat_huy h
            INNER JOIN ct_xuat_huy l ON l.phieu_xuat_huy_id = h.id
            LEFT JOIN san_pham p ON p.id = l.san_pham_id
            WHERE h.trang_thai = 'DA_XUAT_HUY' AND h.duyet_luc >= @fromInclusive AND h.duyet_luc < @toExclusive
            ORDER BY h.id, l.san_pham_id
            """;
        return await QueryAsync(sql, fromInclusive, toExclusive, reader => new ReportWriteOffFact(
            Int64(reader, "writeoff_id"), VietnamDateTimeOffset(reader, "written_off_at"),
            Int64(reader, "product_id"), Int32(reader, "category_id"), Int32(reader, "quantity"),
            RoundedInt64(reader, "unit_cost")), cancellationToken);
    }

    private async Task<IReadOnlyList<T>> QueryAsync<T>(string sql, DateTimeOffset fromInclusive,
        DateTimeOffset toExclusive, Func<DbDataReader, T> map, CancellationToken cancellationToken)
    {
        var connection = _db.Database.GetDbConnection();
        var closeWhenDone = connection.State != System.Data.ConnectionState.Open;
        if (closeWhenDone) await connection.OpenAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@fromInclusive", VietnamDateTime(fromInclusive));
            AddParameter(command, "@toExclusive", VietnamDateTime(toExclusive));
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var results = new List<T>();
            while (await reader.ReadAsync(cancellationToken)) results.Add(map(reader));
            return results;
        }
        finally
        {
            if (closeWhenDone) await connection.CloseAsync();
        }
    }

    private async Task<IReadOnlyList<T>> QueryUnboundedAsync<T>(string sql, Func<DbDataReader, T> map,
        CancellationToken cancellationToken)
    {
        var connection = _db.Database.GetDbConnection();
        var closeWhenDone = connection.State != System.Data.ConnectionState.Open;
        if (closeWhenDone) await connection.OpenAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var results = new List<T>();
            while (await reader.ReadAsync(cancellationToken)) results.Add(map(reader));
            return results;
        }
        finally
        {
            if (closeWhenDone) await connection.CloseAsync();
        }
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static DateTime VietnamDateTime(DateTimeOffset value) =>
        DateTime.SpecifyKind(value.ToOffset(VietnamOffset).DateTime, DateTimeKind.Unspecified);

    private static DateTimeOffset VietnamDateTimeOffset(DbDataReader reader, string name)
    {
        var value = reader[name];
        var date = value switch
        {
            DateTimeOffset dto => dto.ToOffset(VietnamOffset),
            DateTime dt => new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Unspecified), VietnamOffset),
            _ => new DateTimeOffset(Convert.ToDateTime(value, CultureInfo.InvariantCulture), VietnamOffset)
        };
        return date;
    }

    private static string Text(DbDataReader reader, string name) =>
        reader[name] is DBNull ? string.Empty : Convert.ToString(reader[name], CultureInfo.InvariantCulture) ?? string.Empty;

    private static int Int32(DbDataReader reader, string name) =>
        reader[name] is DBNull ? 0 : Convert.ToInt32(reader[name], CultureInfo.InvariantCulture);

    private static long Int64(DbDataReader reader, string name) =>
        reader[name] is DBNull ? 0 : Convert.ToInt64(reader[name], CultureInfo.InvariantCulture);

    private static long RoundedInt64(DbDataReader reader, string name) =>
        reader[name] is DBNull ? 0 : (long)Math.Round(Convert.ToDecimal(reader[name], CultureInfo.InvariantCulture),
            0, MidpointRounding.AwayFromZero);

    private sealed class InvoiceBuilder(long id, DateTimeOffset paidAt, long cashierId, string cashierName, long cashPaid)
    {
        public long Id { get; } = id;
        public DateTimeOffset PaidAt { get; } = paidAt;
        public long CashierId { get; } = cashierId;
        public string CashierName { get; } = cashierName;
        public long CashPaid { get; } = cashPaid;
        public long PromotionDiscount { get; set; }
        public List<ReportInvoiceLineFact> Lines { get; } = [];
    }
}
