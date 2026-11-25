using System.ComponentModel.DataAnnotations;

namespace ConvenienceStore.Web.Modules.Returns.Models;

public sealed class ExchangeRequest : ReturnOrderRequest
{
    [Range(0, long.MaxValue)]
    public long CashReceived { get; set; }
}

public sealed record ExchangeCartLine(
    long ProductId, string Name, int Quantity, long UnitPrice, long Discount)
{
    public long LineTotal => checked(Quantity * (UnitPrice - Discount));
}

public sealed record ExchangePreview(
    ReturnPreview Returned, long NewTotal, long CashDue, string ConfirmationToken,
    IReadOnlyList<ExchangeCartLine> NewItems);

public sealed record ExchangeReceipt(
    long Id, string Code, string OriginalInvoiceCode, long? NewInvoiceId,
    string? NewInvoiceCode, long ReturnValue, long NewTotal, long CashDue,
    long CashReceived, long Change, IReadOnlyList<ReturnPreviewLine> ReturnLines, string Status);

public sealed record ExchangePage(
    ReturnForm Invoice, ExchangeRequest Request, IReadOnlyList<ExchangeCartLine> NewItems,
    ExchangePreview? Preview = null, string? Error = null);
