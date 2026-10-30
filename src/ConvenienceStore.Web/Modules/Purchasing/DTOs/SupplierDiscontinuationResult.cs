namespace ConvenienceStore.Web.Modules.Purchasing.DTOs;

public enum SupplierDiscontinuationStatus
{
    Success, Invalid, NotFound, Conflict, PendingReceipts, HasReceiptHistory
}

public sealed record SupplierDiscontinuationResult(SupplierDiscontinuationStatus Status);
