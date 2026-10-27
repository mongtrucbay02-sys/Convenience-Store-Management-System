using ConvenienceStore.Web.Modules.Reports.Services;

namespace ConvenienceStore.Web.Tests;

public class DashboardServiceTests
{
    private static readonly TimeSpan Vn = TimeSpan.FromHours(7);

    // 20/10/2026 10:00 giờ Việt Nam
    private static readonly DateTimeOffset Now = new(2026, 10, 20, 10, 0, 0, Vn);

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now.ToUniversalTime();
    }

    private sealed class FakeSource(
        IReadOnlyList<PaidInvoiceFact> invoices,
        int lowStock = 0,
        (int, int, int) pending = default) : IDashboardDataSource
    {
        public Task<IReadOnlyList<PaidInvoiceFact>> GetPaidInvoicesAsync(
            DateTimeOffset from, DateTimeOffset to, CancellationToken ct) =>
            Task.FromResult(invoices);

        public Task<int> GetLowStockCountAsync(CancellationToken ct) => Task.FromResult(lowStock);

        public Task<(int Stocktake, int WriteOff, int Returns)> GetPendingApprovalCountsAsync(
            CancellationToken ct) => Task.FromResult(pending);
    }

    private static DashboardService Create(params PaidInvoiceFact[] invoices) =>
        new(new FakeSource(invoices), new FixedTime(Now));

    private static PaidInvoiceFact Inv(
        long id, DateTimeOffset paidAt, long promo = 0, long refund = 0, params InvoiceLineFact[] lines) =>
        new(id, paidAt, lines, promo, refund);

    private static InvoiceLineFact Line(long productId, string name, int qty, long price) =>
        new(productId, name, qty, price);

    [Fact]
    public async Task NoTransactions_ReturnsZerosAndEmptyLists_E1()
    {
        var r = await Create().GetDashboardAsync(default);

        Assert.Equal(0, r.Today.NetRevenue);
        Assert.Equal(0, r.Today.InvoiceCount);
        Assert.Equal(0, r.Today.AverageOrderValue);
        Assert.Null(r.NetRevenueChangePercent);
        Assert.Empty(r.TopProductsToday);
        Assert.Equal(7, r.Last7Days.Count);
        Assert.All(r.Last7Days, d => Assert.Equal(0, d.NetRevenue));
    }

    [Fact]
    public async Task NetRevenue_IsGrossMinusPromotionAndRefund_BR_BC_02()
    {
        var r = await Create(
            Inv(1, Now.AddHours(-1), 3_000, 2_000, Line(1, "A", 2, 10_000), Line(2, "B", 1, 5_000))
        ).GetDashboardAsync(default);

        Assert.Equal(25_000, r.Today.GrossRevenue);
        Assert.Equal(5_000, r.Today.Deductions);
        Assert.Equal(20_000, r.Today.NetRevenue);
        Assert.Equal(1, r.Today.InvoiceCount);
        Assert.Equal(20_000, r.Today.AverageOrderValue);
    }

    [Fact]
    public async Task DayBoundary_UsesVietnamTime_BR_BC_03()
    {
        // 00:30 VN hôm nay = 17:30 UTC hôm qua -> thuộc hôm nay.
        var justAfterMidnight = new DateTimeOffset(2026, 10, 20, 0, 30, 0, Vn);
        // 23:30 VN hôm qua = 16:30 UTC hôm qua -> thuộc hôm qua.
        var justBeforeMidnight = new DateTimeOffset(2026, 10, 19, 23, 30, 0, Vn);

        var r = await Create(
            Inv(1, justAfterMidnight.ToUniversalTime(), 0, 0, Line(1, "A", 1, 10_000)),
            Inv(2, justBeforeMidnight.ToUniversalTime(), 0, 0, Line(1, "A", 1, 4_000))
        ).GetDashboardAsync(default);

        Assert.Equal(10_000, r.Today.NetRevenue);
        Assert.Equal(4_000, r.Yesterday.NetRevenue);
        Assert.Equal(150m, r.NetRevenueChangePercent);
    }

    [Fact]
    public async Task UsesListPriceFromInvoiceLine_BR_BC_04()
    {
        var r = await Create(
            Inv(1, Now.AddHours(-2), 0, 0, Line(1, "A", 3, 7_000))
        ).GetDashboardAsync(default);

        Assert.Equal(21_000, r.Today.NetRevenue);
        Assert.Equal(21_000, r.TopProductsToday.Single().Revenue);
    }

    [Fact]
    public async Task TopProducts_AreTodayOnly_SortedByQuantity_MaxFive()
    {
        var lines = Enumerable.Range(1, 6)
            .Select(i => Line(i, $"P{i}", i, 1_000))
            .ToArray();

        var r = await Create(
            Inv(1, Now.AddHours(-1), 0, 0, lines),
            Inv(2, Now.AddDays(-1), 0, 0, Line(99, "Hôm qua", 100, 1_000))
        ).GetDashboardAsync(default);

        Assert.Equal(5, r.TopProductsToday.Count);
        Assert.Equal(new long[] { 6, 5, 4, 3, 2 }, r.TopProductsToday.Select(p => p.ProductId));
    }

    [Fact]
    public async Task Last7Days_IncludesTodayInAscendingOrder()
    {
        var r = await Create(
            Inv(1, Now.AddDays(-6), 0, 0, Line(1, "A", 1, 1_000)),
            Inv(2, Now, 0, 0, Line(1, "A", 1, 2_000))
        ).GetDashboardAsync(default);

        Assert.Equal(new DateOnly(2026, 10, 14), r.Last7Days[0].Date);
        Assert.Equal(new DateOnly(2026, 10, 20), r.Last7Days[6].Date);
        Assert.Equal(1_000, r.Last7Days[0].NetRevenue);
        Assert.Equal(2_000, r.Last7Days[6].NetRevenue);
    }

    [Fact]
    public async Task LowStockAndPendingApprovals_ComeFromDataSource()
    {
        var svc = new DashboardService(
            new FakeSource(Array.Empty<PaidInvoiceFact>(), lowStock: 4, pending: (1, 2, 3)),
            new FixedTime(Now));

        var r = await svc.GetDashboardAsync(default);

        Assert.Equal(4, r.LowStockCount);
        Assert.Equal((1, 2, 3), (r.PendingApprovals.Stocktake, r.PendingApprovals.WriteOff, r.PendingApprovals.Returns));
    }

    [Fact]
    public void AdminOnlyAttribute_IsAppliedToBothControllers()
    {
        foreach (var t in new[]
        {
            typeof(ConvenienceStore.Web.Modules.Reports.Controllers.DashboardApiController),
            typeof(ConvenienceStore.Web.Modules.Reports.Controllers.ReportsController),
        })
        {
            var attr = (Microsoft.AspNetCore.Authorization.AuthorizeAttribute?)Attribute.GetCustomAttribute(
                t, typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute));
            Assert.Equal(ConvenienceStore.Web.Modules.Reports.AuthorizationPolicies.AdminOnly, attr?.Policy);
        }
    }
}
