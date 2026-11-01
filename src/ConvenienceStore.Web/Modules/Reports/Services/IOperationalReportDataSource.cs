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
public sealed record ReportPurchaseReceiptLineFact(long ProductId, string ProductName, int Quantity, long UnitCost);
public sealed record ReportPurchaseReceiptFact(long Id, DateTimeOffset ReceivedAt, int SupplierId, string SupplierName,
    IReadOnlyList<ReportPurchaseReceiptLineFact> Lines);
public sealed record ReportReturnedLineFact(long ProductId, string ProductName, int CategoryId, string CategoryName,
    int Quantity, long UnitCostSnapshot, int RestockedQuantity, long RefundAmount, long DestroyedValue = 0);
public sealed record ReportReturnFact(long Id, long InvoiceId, DateTimeOffset ReturnedAt,
    IReadOnlyList<ReportReturnedLineFact> Lines, long CashierId = 0, string CashierName = "");
public sealed record ReportWriteOffFact(long Id, DateTimeOffset WrittenOffAt, long ProductId, int CategoryId,
    int Quantity, long UnitCostSnapshot);

/// <summary>
/// Report input boundary for BC-02 through BC-08. The production adapter queries the shared MySQL schema;
/// the sample adapter remains available for isolated deterministic tests.
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
    Task<IReadOnlyList<ReportPurchaseReceiptFact>> GetPurchaseReceiptsAsync(
        DateTimeOffset fromInclusive, DateTimeOffset toExclusive, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ReportPurchaseReceiptFact>>([]);
    Task<IReadOnlyList<ReportReturnFact>> GetReturnsAsync(
        DateTimeOffset fromInclusive, DateTimeOffset toExclusive, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ReportReturnFact>>([]);
}

/// <summary>Live transactional tables used by BC-07/08 once the shared MySQL schema is available.</summary>
public interface ITransactionalReportsDataSource
{
    Task<IReadOnlyList<ReportPaidInvoiceFact>> GetPaidInvoicesAsync(
        DateTimeOffset fromInclusive, DateTimeOffset toExclusive, CancellationToken cancellationToken);
    Task<IReadOnlyList<ReportPurchaseReceiptFact>> GetPurchaseReceiptsAsync(
        DateTimeOffset fromInclusive, DateTimeOffset toExclusive, CancellationToken cancellationToken);
    Task<IReadOnlyList<ReportReturnFact>> GetReturnsAsync(
        DateTimeOffset fromInclusive, DateTimeOffset toExclusive, CancellationToken cancellationToken);
    Task<IReadOnlyList<ReportWriteOffFact>> GetWriteOffsAsync(
        DateTimeOffset fromInclusive, DateTimeOffset toExclusive, CancellationToken cancellationToken);
}

