namespace ConvenienceStore.Web.Modules.Reports.Models;

/// <summary>Response của GET /api/reports/dashboard (BC-01). Tiền là số nguyên VND.</summary>
public sealed record DashboardResponse(
    DateTimeOffset AsOf,
    DayStatsDto Today,
    DayStatsDto Yesterday,
    decimal? NetRevenueChangePercent,
    IReadOnlyList<DailyRevenueDto> Last7Days,
    IReadOnlyList<TopProductDto> TopProductsToday,
    int LowStockCount,
    PendingApprovalsDto PendingApprovals);

public sealed record DayStatsDto(
    long GrossRevenue,
    long Deductions,
    long NetRevenue,
    int InvoiceCount,
    long AverageOrderValue);

public sealed record DailyRevenueDto(DateOnly Date, long NetRevenue);

public sealed record TopProductDto(
    long ProductId,
    string Name,
    int QuantitySold,
    long Revenue);

public sealed record PendingApprovalsDto(int Stocktake, int WriteOff, int Returns);
