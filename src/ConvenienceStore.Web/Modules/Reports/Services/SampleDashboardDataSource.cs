namespace ConvenienceStore.Web.Modules.Reports.Services;

/// <summary>Dữ liệu mẫu cố định theo ngày, dùng trong tuần 4 (xem issue #4).</summary>
public sealed class SampleDashboardDataSource : IDashboardDataSource
{
    private static readonly (long Id, string Name, long Price)[] Products =
    {
        (1, "Nước suối 500ml", 5_000),
        (2, "Mì gói", 4_500),
        (3, "Sữa tươi 1L", 32_000),
        (4, "Bánh mì sandwich", 18_000),
        (5, "Cà phê lon", 15_000),
        (6, "Kẹo cao su", 8_000),
    };

    private readonly TimeProvider _timeProvider;

    public SampleDashboardDataSource(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public Task<IReadOnlyList<PaidInvoiceFact>> GetPaidInvoicesAsync(
        DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
    {
        var offset = TimeSpan.FromHours(7);
        var now = _timeProvider.GetUtcNow();
        var invoices = new List<PaidInvoiceFact>();
        var id = 1L;

        for (var daysAgo = 7; daysAgo >= 0; daysAgo--)
        {
            var day = now.ToOffset(offset).Date.AddDays(-daysAgo);
            var count = 6 + (daysAgo * 3 % 5);
            for (var n = 0; n < count; n++, id++)
            {
                var paidAt = new DateTimeOffset(day.AddHours(7).AddMinutes(n * 37 % 600), offset);
                if (paidAt > now) continue;
                var lines = Enumerable.Range(0, 1 + (n % 3))
                    .Select(i => Products[(n + i + daysAgo) % Products.Length])
                    .Select(p => new InvoiceLineFact(p.Id, p.Name, 1 + (n % 4), p.Price))
                    .ToList();
                var promo = n % 4 == 0 ? 2_000 : 0;
                var refund = n % 9 == 8 ? 5_000 : 0;
                invoices.Add(new PaidInvoiceFact(id, paidAt, lines, promo, refund));
            }
        }

        IReadOnlyList<PaidInvoiceFact> result = invoices
            .Where(i => i.PaidAt >= from && i.PaidAt < to)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<int> GetLowStockCountAsync(CancellationToken cancellationToken) =>
        Task.FromResult(3);

    public Task<(int Stocktake, int WriteOff, int Returns)> GetPendingApprovalCountsAsync(
        CancellationToken cancellationToken) =>
        Task.FromResult((1, 2, 0));
}
