namespace ConvenienceStore.Web.Modules.Reports.Services;

/// <summary>Một dòng hóa đơn. ListPrice là bản sao giá lưu trên hóa đơn (BR-BC-04).</summary>
public sealed record InvoiceLineFact(
    long ProductId,
    string ProductName,
    int Quantity,
    long ListPrice);

/// <summary>Hóa đơn "Đã thanh toán" (BR-BC-01). Giảm trừ = khuyến mãi + hoàn trả (BR-BC-02).</summary>
public sealed record PaidInvoiceFact(
    long InvoiceId,
    DateTimeOffset PaidAt,
    IReadOnlyList<InvoiceLineFact> Lines,
    long PromotionDiscount,
    long RefundAmount,
    bool IsReturn = false);

/// <summary>
/// Data source for dashboard revenue and operational approval counts.
/// </summary>
public interface IDashboardDataSource
{
    /// <summary>Chỉ trả hóa đơn "Đã thanh toán" có PaidAt trong [from, to).</summary>
    Task<IReadOnlyList<PaidInvoiceFact>> GetPaidInvoicesAsync(
        DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken);

    Task<int> GetLowStockCountAsync(CancellationToken cancellationToken);

    Task<(int Stocktake, int WriteOff, int Returns)> GetPendingApprovalCountsAsync(
        CancellationToken cancellationToken);
}
