namespace ConvenienceStore.Web.Modules.Reports.Services;

public sealed record ReportProductFact(
    long Id, string Name, int CategoryId, string CategoryName, long UnitCost, int ReorderPoint,
    int CurrentStock);

public sealed record ReportInvoiceLineFact(
    long ProductId, string ProductName, int CategoryId, string CategoryName,
    int Quantity, long ListPriceSnapshot, long UnitCostSnapshot);

/// <summary>Only paid invoices are returned; the copied line prices are the report source of truth.</summary>
public sealed record ReportPaidInvoiceFact(
    long Id, DateTimeOffset PaidAt, long CashierId, string CashierName,
    IReadOnlyList<ReportInvoiceLineFact> Lines, long PromotionDiscount, long RefundAmount,
    long CashPaid);

public sealed record ReportCancelledOrderFact(long Id, DateTimeOffset CancelledAt, long CashierId, string CashierName);
public sealed record ReportVoidedInvoiceFact(long Id, DateTimeOffset VoidedAt, long CashierId, string CashierName);
public sealed record StockMovementFact(long ProductId, DateTimeOffset OccurredAt, int Quantity, bool IsInbound);

/// <summary>
/// Report input boundary. The main branch has no transaction entities/tables yet, so the registered
/// implementation is deterministic demo data; replace it with direct EF queries when NH/POS/KHO land.
/// </summary>
public interface IOperationalReportDataSource
{
    Task<IReadOnlyList<ReportProductFact>> GetProductsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<ReportPaidInvoiceFact>> GetPaidInvoicesAsync(
        DateTimeOffset fromInclusive, DateTimeOffset toExclusive, CancellationToken cancellationToken);
    Task<IReadOnlyList<ReportCancelledOrderFact>> GetCancelledOrdersAsync(
        DateTimeOffset fromInclusive, DateTimeOffset toExclusive, CancellationToken cancellationToken);
    Task<IReadOnlyList<ReportVoidedInvoiceFact>> GetVoidedInvoicesAsync(
        DateTimeOffset fromInclusive, DateTimeOffset toExclusive, CancellationToken cancellationToken);
    Task<IReadOnlyList<StockMovementFact>> GetStockMovementsAsync(
        DateTimeOffset fromInclusive, DateTimeOffset toExclusive, CancellationToken cancellationToken);
}
