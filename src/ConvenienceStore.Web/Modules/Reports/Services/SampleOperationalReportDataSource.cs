namespace ConvenienceStore.Web.Modules.Reports.Services;

/// <summary>
/// Repeatable 60-day fixture for the report pages. It deliberately sits behind an interface so it can
/// be replaced by direct transaction/stock-card queries once those schemas are merged into main.
/// </summary>
public sealed class SampleOperationalReportDataSource : IOperationalReportDataSource
{
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);
    private static readonly (int Id, string Name)[] Categories =
    [
        (1, "Đồ uống"), (2, "Thực phẩm khô"), (3, "Sữa và lạnh"), (4, "Chăm sóc cá nhân")
    ];
    private static readonly (long Id, string Name, int Category, long Price, long Cost, int Reorder)[] Catalog =
    [
        (1, "Nước suối 500ml", 1, 5_000, 2_800, 20),
        (2, "Cà phê lon", 1, 15_000, 9_000, 12),
        (3, "Trà xanh chai", 1, 10_000, 5_500, 15),
        (4, "Mì gói", 2, 4_500, 2_700, 30),
        (5, "Bánh quy", 2, 18_000, 11_000, 10),
        (6, "Gạo túi 2kg", 2, 48_000, 38_000, 8),
        (7, "Sữa tươi 1L", 3, 32_000, 24_000, 10),
        (8, "Sữa chua lốc", 3, 28_000, 20_000, 8),
        (9, "Bơ hộp", 3, 42_000, 31_000, 6),
        (10, "Khăn giấy", 4, 12_000, 6_000, 15),
        (11, "Kem đánh răng", 4, 25_000, 16_000, 8),
        (12, "Dầu gội gói", 4, 3_000, 1_500, 25)
    ];
    private static readonly (long Id, string Name)[] Cashiers =
    [(1, "Nguyễn An"), (2, "Trần Bình"), (3, "Lê Chi")];

    private readonly IReadOnlyList<ReportProductFact> _products;
    private readonly IReadOnlyList<ReportPaidInvoiceFact> _invoices;
    private readonly IReadOnlyList<ReportCancelledOrderFact> _cancelledOrders;
    private readonly IReadOnlyList<ReportVoidedInvoiceFact> _voidedInvoices;
    private readonly IReadOnlyList<StockMovementFact> _movements;

    public SampleOperationalReportDataSource(TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow().ToOffset(VietnamOffset);
        var firstDay = DateOnly.FromDateTime(now.DateTime).AddDays(-59);
        var stock = Catalog.ToDictionary(p => p.Id, _ => 35);
        var movements = new List<StockMovementFact>();
        var invoices = new List<ReportPaidInvoiceFact>();
        var cancelled = new List<ReportCancelledOrderFact>();
        var voided = new List<ReportVoidedInvoiceFact>();
        long invoiceId = 1;
        long cancellationId = 1;

        for (var dayIndex = 0; dayIndex < 60; dayIndex++)
        {
            var date = firstDay.AddDays(dayIndex);
            var dateTime = date.ToDateTime(TimeOnly.MinValue);

            // Demo receipts are dated every ten days and stock-card movements reconcile to closing stock.
            if (dayIndex % 10 == 0)
            {
                for (var p = 0; p < Catalog.Length; p++)
                {
                    var product = Catalog[p];
                    var quantity = 16 + (p % 5) * 3;
                    stock[product.Id] += quantity;
                    movements.Add(new StockMovementFact(
                        product.Id, new DateTimeOffset(dateTime.AddHours(6), VietnamOffset), quantity, true));
                }
            }

            var invoiceCount = 4 + (dayIndex * 7 % 5);
            for (var n = 0; n < invoiceCount; n++)
            {
                var paidAt = new DateTimeOffset(dateTime.AddHours(8 + n % 10).AddMinutes((n * 19) % 60), VietnamOffset);
                if (paidAt > now) continue;

                var lineCount = 1 + (n % 2);
                var lines = new List<ReportInvoiceLineFact>();
                for (var j = 0; j < lineCount; j++)
                {
                    var catalogIndex = (dayIndex * 3 + n * 2 + j * 5) % Catalog.Length;
                    var item = Catalog[catalogIndex];
                    var qty = Math.Min(1 + ((dayIndex + n + j) % 3), stock[item.Id]);
                    if (qty <= 0) continue;
                    stock[item.Id] -= qty;
                    movements.Add(new StockMovementFact(item.Id, paidAt, qty, false));
                    lines.Add(new ReportInvoiceLineFact(
                        item.Id, item.Name, item.Category, Categories[item.Category - 1].Name,
                        qty, item.Price, item.Cost));
                }

                if (lines.Count == 0) continue;
                var cashier = Cashiers[(dayIndex + n) % Cashiers.Length];
                var gross = lines.Sum(l => (long)l.Quantity * l.ListPriceSnapshot);
                var promotion = n % 4 == 0 ? Math.Min(2_000, gross) : 0;
                var refund = n % 13 == 12 ? Math.Min(3_000, gross - promotion) : 0;
                var net = gross - promotion - refund;
                invoices.Add(new ReportPaidInvoiceFact(
                    invoiceId++, paidAt, cashier.Id, cashier.Name, lines, promotion, refund, net));

                if (n % 9 == 8)
                    voided.Add(new ReportVoidedInvoiceFact(invoiceId + 100_000,
                        paidAt.AddMinutes(20), cashier.Id, cashier.Name));
            }

            if (dayIndex % 3 == 0)
            {
                var cashier = Cashiers[dayIndex % Cashiers.Length];
                cancelled.Add(new ReportCancelledOrderFact(
                    cancellationId++, new DateTimeOffset(dateTime.AddHours(17), VietnamOffset), cashier.Id, cashier.Name));
            }
        }

        _products = Catalog.Select(p => new ReportProductFact(
            p.Id, p.Name, p.Category, Categories[p.Category - 1].Name,
            p.Cost, p.Reorder, stock[p.Id])).ToArray();
        _invoices = invoices;
        _cancelledOrders = cancelled;
        _voidedInvoices = voided;
        _movements = movements;
    }

    public Task<IReadOnlyList<ReportProductFact>> GetProductsAsync(CancellationToken cancellationToken) =>
        Task.FromResult(_products);

    public Task<IReadOnlyList<ReportPaidInvoiceFact>> GetPaidInvoicesAsync(
        DateTimeOffset fromInclusive, DateTimeOffset toExclusive, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ReportPaidInvoiceFact>>(
            _invoices.Where(i => i.PaidAt >= fromInclusive && i.PaidAt < toExclusive).ToArray());

    public Task<IReadOnlyList<ReportCancelledOrderFact>> GetCancelledOrdersAsync(
        DateTimeOffset fromInclusive, DateTimeOffset toExclusive, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ReportCancelledOrderFact>>(
            _cancelledOrders.Where(i => i.CancelledAt >= fromInclusive && i.CancelledAt < toExclusive).ToArray());

    public Task<IReadOnlyList<ReportVoidedInvoiceFact>> GetVoidedInvoicesAsync(
        DateTimeOffset fromInclusive, DateTimeOffset toExclusive, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ReportVoidedInvoiceFact>>(
            _voidedInvoices.Where(i => i.VoidedAt >= fromInclusive && i.VoidedAt < toExclusive).ToArray());

    public Task<IReadOnlyList<StockMovementFact>> GetStockMovementsAsync(
        DateTimeOffset fromInclusive, DateTimeOffset toExclusive, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<StockMovementFact>>(
            _movements.Where(m => m.OccurredAt >= fromInclusive && m.OccurredAt < toExclusive).ToArray());
}
