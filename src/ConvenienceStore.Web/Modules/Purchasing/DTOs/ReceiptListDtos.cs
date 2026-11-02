namespace ConvenienceStore.Web.Modules.Purchasing.DTOs;

public sealed record ReceiptListQuery(string? Code = null, uint? SupplierId = null,
    DateOnly? From = null, DateOnly? To = null, string? Status = null);

public sealed record ReceiptListItemDto(uint Id, string Code, DateOnly Date,
    uint SupplierId, string SupplierName, ulong TotalAmount,
    string Status, uint CreatorId, string CreatorName);