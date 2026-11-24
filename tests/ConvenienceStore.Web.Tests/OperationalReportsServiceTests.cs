using ConvenienceStore.Web.Modules.Reports.Models;
using ConvenienceStore.Web.Modules.Reports.Services;

namespace ConvenienceStore.Web.Tests;

public sealed class OperationalReportsServiceTests
{
    private static readonly TimeSpan Vn = TimeSpan.FromHours(7);
    private static readonly DateTimeOffset Now = new(2026, 11, 2, 10, 0, 0, Vn);

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now.ToUniversalTime();
    }

    private sealed class FakeSource : IOperationalReportDataSource, ITransactionalReportsDataSource
    {
        public IReadOnlyList<ReportProductFact> Products { get; } =
        [
            new(1, "Nước", 1, "Đồ uống", 40, 5, 13),
            new(2, "Mì", 2, "Thực phẩm khô", 25, 3, 0)
        ];

        public IReadOnlyList<ReportPaidInvoiceFact> Invoices { get; init; } =
        [
            new(10, new DateTimeOffset(2026, 11, 1, 0, 30, 0, Vn), 7, "An",
                [new(1, "Nước", 1, "Đồ uống", 2, 100, 40), new(2, "Mì", 2, "Thực phẩm khô", 1, 50, 25)],
                20, 0, 220)
        ];

        public IReadOnlyList<ReportReturnFact> Returns { get; init; } =
        [
            new(12, 10, new DateTimeOffset(2026, 11, 1, 12, 0, 0, Vn),
                [new(1, "Nước", 1, "Đồ uống", 1, 40, 1, 10)], 7, "An")
        ];

        public IReadOnlyList<StockMovementFact> Movements { get; } =
        [
            new(1, new DateTimeOffset(2026, 10, 31, 6, 0, 0, Vn), 10, true),
            new(1, new DateTimeOffset(2026, 11, 1, 0, 45, 0, Vn), 4, false),
            new(1, new DateTimeOffset(2026, 11, 2, 8, 0, 0, Vn), 2, true)
        ];

        public Task<IReadOnlyList<ReportProductFact>> GetProductsAsync(CancellationToken ct) => Task.FromResult(Products);
        public Task<IReadOnlyList<ReportPurchaseReceiptFact>> GetPurchaseReceiptsAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ReportPurchaseReceiptFact>>([]);
        public Task<IReadOnlyList<ReportWriteOffFact>> GetWriteOffsAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ReportWriteOffFact>>([]);
        public Task<IReadOnlyList<ReportPaidInvoiceFact>> GetPaidInvoicesAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ReportPaidInvoiceFact>>(Invoices.Where(x => x.PaidAt >= from && x.PaidAt < to).ToArray());
        public Task<IReadOnlyList<ReportCancelledOrderFact>> GetCancelledOrdersAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ReportCancelledOrderFact>>([new(1, new DateTimeOffset(2026, 11, 1, 9, 0, 0, Vn), 7, "An")]);
        public Task<IReadOnlyList<ReportVoidedInvoiceFact>> GetVoidedInvoicesAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ReportVoidedInvoiceFact>>([new(11, new DateTimeOffset(2026, 11, 1, 9, 30, 0, Vn), 7, "An")]);
        public Task<IReadOnlyList<ReportReturnFact>> GetReturnsAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ReportReturnFact>>(Returns.Where(x => x.ReturnedAt >= from && x.ReturnedAt < to).ToArray());
        public Task<IReadOnlyList<StockMovementFact>> GetStockMovementsAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<StockMovementFact>>(Movements.Where(x => x.OccurredAt >= from && x.OccurredAt < to).ToArray());
    }

    private static OperationalReportsService Create() => new(new FakeSource(), new FixedTime(Now));
    private static ReportQuery OneDay() => new() { Period = "custom", From = new DateOnly(2026, 10, 31), To = new DateOnly(2026, 11, 1) };

    [Fact]
    public async Task Revenue_UsesPaidAtInVietnamAndReconcilesGrossDeductionsNet()
    {
        var report = await Create().GetRevenueAsync(OneDay(), default);

        Assert.Equal(250, report.Metrics[1].Value);
        Assert.Equal(30, report.Metrics[2].Value);
        Assert.Equal(220, report.Metrics[3].Value);
        Assert.Equal(1, report.Metrics[0].Value);
        Assert.Equal(220, report.Metrics[4].Value);
    }

    [Fact]
    public async Task ProductAndCategoryReports_AllocateInvoiceDeductionsProportionally()
    {
        var products = await Create().GetProductsAsync(OneDay(), default);
        var categories = await Create().GetCategoriesAsync(OneDay(), default);

        Assert.Contains(products.Rows, row => row.Key == "product:1" && row.Cells[4] == "174 ₫");
        Assert.Contains(categories.Rows, row => row.Key == "category:1" && row.Cells[4] == "174 ₫");
        Assert.Equal(220, products.Metrics.Single(x => x.Label == "Doanh thu thuần").Value);
    }

    [Fact]
    public async Task GrossProfitAndProductReportUseSameDiscountRemainderAfterFiltering()
    {
        var data = new FakeSource();
        var service = new OperationalReportsService(new FakeSource
        {
            Invoices = [data.Invoices[0] with { PromotionDiscount = 21 }]
        }, new FixedTime(Now));
        var query = OneDay();
        query.ProductId = 2;
        var products = await service.GetProductsAsync(query, default);
        var profit = await service.GetGrossProfitAsync(query, default);
        Assert.Equal(45, products.Metrics.Single(x => x.Label == "Doanh thu thuần").Value);
        Assert.Equal(45, profit.Metrics.Single(x => x.Label == "Doanh thu thuần").Value);
    }

    [Theory]
    [InlineData("BC-02", "2026-11-02")]
    [InlineData("BC-03", "product:1")]
    [InlineData("BC-04", "category:1")]
    [InlineData("BC-05", "cashier:7")]
    public async Task DetailIncludesReturnOfPriorPeriodSale(string code, string key)
    {
        var data = new FakeSource();
        var service = new OperationalReportsService(new FakeSource
        {
            Returns = [data.Returns[0] with { ReturnedAt = Now }]
        }, new FixedTime(Now));
        var query = OneDay();
        query.From = query.To = new DateOnly(2026, 11, 2);
        var detail = await service.GetDetailsAsync(code, key, query, default);
        Assert.Equal(-10, detail.Metrics.Single(x => x.Label == "Doanh thu thuần").Value);
        Assert.Equal("return:12", Assert.Single(detail.Rows).Key);
    }

    [Fact]
    public async Task CategoryFilterAppliesToBothSalesAndReturns()
    {
        var query = OneDay();
        query.CategoryId = 1;
        var report = await Create().GetCategoriesAsync(query, default);
        Assert.Equal(174, report.Metrics.Single(x => x.Label == "Doanh thu thuần").Value);
        Assert.DoesNotContain(report.Rows, x => x.Key == "category:2");
        Assert.Equal(174, report.Chart.Sum(x => x.Value));
    }

    [Theory]
    [InlineData("BC-02", "2026-11-01", 220)]
    [InlineData("BC-03", "product:1", 174)]
    [InlineData("BC-04", "category:1", 174)]
    [InlineData("BC-05", "cashier:7", 220)]
    public async Task DetailReconcilesFilteredSalesAndCompletedReturns(string code, string key, long expected)
    {
        var report = await Create().GetDetailsAsync(code, key, OneDay(), default);
        Assert.Equal(expected, report.Metrics.Single(x => x.Label == "Doanh thu thuần").Value);
        Assert.Contains(report.Rows, x => x.Key == "return:12");
        Assert.All(report.Rows, x => Assert.Equal(report.Columns.Count, x.Cells.Count));
    }

    [Theory]
    [InlineData("BC-03")]
    [InlineData("BC-04")]
    public async Task FilterWithoutMatchingTransactionsHasEmptyState(string code)
    {
        var query = OneDay();
        query.CategoryId = 999;
        var report = code == "BC-03"
            ? await Create().GetProductsAsync(query, default)
            : await Create().GetCategoriesAsync(query, default);
        Assert.Equal("Không có dữ liệu trong khoảng đã chọn", report.Message);
        Assert.Empty(report.Chart);
    }

    [Fact]
    public async Task CashierReport_IncludesCancelledAndVoidedCounts()
    {
        var report = await Create().GetCashiersAsync(OneDay(), default);

        Assert.Equal(1, report.Metrics.Single(x => x.Label == "Đơn hủy trước thanh toán").Value);
        Assert.Equal(1, report.Metrics.Single(x => x.Label == "Hóa đơn hủy sau thanh toán").Value);
        Assert.Equal(220, report.Metrics.Single(x => x.Label == "Tiền mặt thu").Value);
    }

    [Fact]
    public async Task InventoryReport_ReconcilesOpeningPlusInMinusOutToClosing()
    {
        var report = await Create().GetInventoryAsync(OneDay(), default);
        var row = Assert.Single(report.Rows, x => x.Key == "product:1");

        var opening = int.Parse(row.Cells[6]);
        var incoming = int.Parse(row.Cells[7]);
        var outgoing = int.Parse(row.Cells[8]);
        var closing = int.Parse(row.Cells[9]);
        Assert.Equal(opening + incoming - outgoing, closing);
        Assert.Equal(5, opening);
        Assert.Equal(10, incoming);
        Assert.Equal(4, outgoing);
        Assert.Equal(11, closing);
    }

    [Theory]
    [InlineData("2026-11-02", "2026-11-01", "Ngày bắt đầu phải trước hoặc bằng ngày kết thúc.")]
    [InlineData("2025-10-31", "2026-11-01", "Khoảng thời gian không được dài hơn 366 ngày.")]
    public void RangeResolver_RejectsInvalidRanges(string fromText, string toText, string expected)
    {
        var query = new ReportQuery { Period = "custom", From = DateOnly.Parse(fromText), To = DateOnly.Parse(toText) };

        Assert.False(ReportRangeResolver.TryResolve(query, Now, out _, out var error));
        Assert.Equal(expected, error);
    }
}
