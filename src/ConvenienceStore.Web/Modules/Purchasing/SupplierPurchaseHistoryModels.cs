using ConvenienceStore.Web.Data.Entities;

namespace ConvenienceStore.Web.Modules.Purchasing;

/// <summary>NH-09: lịch sử nhập kho của một nhà cung cấp, chỉ tính phiếu đã nhập kho.</summary>
public sealed record SupplierPurchaseHistory(
    IReadOnlyList<NhaCungCap> Suppliers,
    NhaCungCap? Supplier,
    DateOnly? From,
    DateOnly? To,
    int ReceiptCount,
    long TotalValue,
    IReadOnlyList<SupplierPurchaseReceiptRow> Receipts,
    bool HasMoreReceipts,
    IReadOnlyList<SupplierPurchaseProductRow> Products)
{
    public const int MaxReceipts = 50;
    public const int MaxProducts = 20;
}

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
