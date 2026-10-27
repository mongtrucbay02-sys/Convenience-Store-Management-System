using ConvenienceStore.Web.Modules.Reports.Models;

namespace ConvenienceStore.Web.Modules.Reports.Services;

public sealed class DashboardService : IDashboardService
{
    // BR-BC-03: ngày giao dịch theo giờ Việt Nam (UTC+7, không có giờ mùa hè).
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);
    private const int TopProductCount = 5;
    private const int TrendDays = 7;

    private readonly IDashboardDataSource _dataSource;
    private readonly TimeProvider _timeProvider;

    public DashboardService(IDashboardDataSource dataSource, TimeProvider timeProvider)
    {
        _dataSource = dataSource;
        _timeProvider = timeProvider;
    }

    public async Task<DashboardResponse> GetDashboardAsync(CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow().ToOffset(VietnamOffset);
        var today = DateOnly.FromDateTime(now.DateTime);
        var firstDay = today.AddDays(-(TrendDays - 1));

        var from = StartOfDay(firstDay);
        var to = StartOfDay(today.AddDays(1));

        var invoices = await _dataSource.GetPaidInvoicesAsync(from, to, cancellationToken);
        var lowStock = await _dataSource.GetLowStockCountAsync(cancellationToken);
        var pending = await _dataSource.GetPendingApprovalCountsAsync(cancellationToken);

        // BR-BC-01 được nguồn dữ liệu bảo đảm; lọc lại theo khoảng để an toàn.
        var byDay = invoices
            .Where(i => i.PaidAt >= from && i.PaidAt < to)
            .GroupBy(i => DateOnly.FromDateTime(i.PaidAt.ToOffset(VietnamOffset).DateTime))
            .ToDictionary(g => g.Key, g => g.ToList());

        List<PaidInvoiceFact> ForDay(DateOnly d) =>
            byDay.TryGetValue(d, out var list) ? list : new List<PaidInvoiceFact>();

        var todayStats = BuildStats(ForDay(today));
        var yesterdayStats = BuildStats(ForDay(today.AddDays(-1)));

        decimal? change = yesterdayStats.NetRevenue == 0
            ? null
            : Math.Round(
                (todayStats.NetRevenue - yesterdayStats.NetRevenue) * 100m / yesterdayStats.NetRevenue,
                1,
                MidpointRounding.AwayFromZero);

        var last7 = Enumerable.Range(0, TrendDays)
            .Select(i => firstDay.AddDays(i))
            .Select(d => new DailyRevenueDto(d, BuildStats(ForDay(d)).NetRevenue))
            .ToList();

        var top = ForDay(today)
            .SelectMany(i => i.Lines)
            .GroupBy(l => l.ProductId)
            .Select(g => new TopProductDto(
                g.Key,
                g.First().ProductName,
                g.Sum(l => l.Quantity),
                g.Sum(l => (long)l.Quantity * l.ListPrice)))
            .OrderByDescending(p => p.QuantitySold)
            .ThenByDescending(p => p.Revenue)
            .ThenBy(p => p.Name, StringComparer.Ordinal)
            .Take(TopProductCount)
            .ToList();

        return new DashboardResponse(
            now,
            todayStats,
            yesterdayStats,
            change,
            last7,
            top,
            lowStock,
            new PendingApprovalsDto(pending.Stocktake, pending.WriteOff, pending.Returns));
    }

    // BR-BC-02: gộp = Σ(số lượng × giá niêm yết); giảm trừ = khuyến mãi + hoàn trả; thuần = gộp − giảm trừ.
    private static DayStatsDto BuildStats(IReadOnlyCollection<PaidInvoiceFact> invoices)
    {
        var gross = invoices.Sum(i => i.Lines.Sum(l => (long)l.Quantity * l.ListPrice));
        var deductions = invoices.Sum(i => i.PromotionDiscount + i.RefundAmount);
        var net = gross - deductions;
        var average = invoices.Count == 0
            ? 0
            : (long)Math.Round((decimal)net / invoices.Count, MidpointRounding.AwayFromZero);
        return new DayStatsDto(gross, deductions, net, invoices.Count, average);
    }

    private static DateTimeOffset StartOfDay(DateOnly date) =>
        new(date.ToDateTime(TimeOnly.MinValue), VietnamOffset);
}
