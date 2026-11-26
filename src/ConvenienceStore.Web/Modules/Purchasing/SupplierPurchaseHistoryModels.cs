namespace ConvenienceStore.Web.Modules.Purchasing;

/// <summary>NH-09: lịch sử nhập kho của một nhà cung cấp, chỉ tính phiếu đã nhập kho.</summary>
public sealed record SupplierPurchaseHistory(
    IReadOnlyList<SupplierPurchaseSupplier> Suppliers,
    SupplierPurchaseSupplier? Supplier,
    DateOnly? From,
    DateOnly? To,
    int ReceiptCount,
    long TotalValue,
    IReadOnlyList<SupplierPurchaseReceiptRow> Receipts,
    bool HasMoreReceipts,
    IReadOnlyList<SupplierPurchaseProductRow> Products,
    int Page)
{
    public const int ReceiptsPerPage = 50;
    public const int MaxPage = 100000;
}

public sealed record SupplierPurchaseSupplier(long Id, string Code, string Name);

public sealed record SupplierPurchaseReceiptRow(long Id, string Code, DateOnly ReceivedOn, int LineCount, long Total);

public sealed record SupplierPurchaseProductRow(
    long ProductId,
    string ProductName,
    string Unit,
    int ReceiptCount,
    long TotalQuantity,
    long TotalValue,
    long MinPrice,
    long MaxPrice,
    long LatestPrice,
    DateOnly LatestReceivedOn);
