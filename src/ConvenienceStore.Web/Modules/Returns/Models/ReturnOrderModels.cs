using System.Text.Json.Serialization;

namespace ConvenienceStore.Web.Modules.Returns.Models;

public sealed class ReturnLineRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public string Reason { get; set; } = "";
    public string? Note { get; set; }
    public string Condition { get; set; } = "";
    public string Handling { get; set; } = "NHAP_LAI";
}

public sealed class ExchangeRequest
{
    public long InvoiceId { get; set; }
    public List<ExchangeLineRequest> Lines { get; set; } = [];
    public long CashReceived { get; set; }
}

public sealed class ExchangeLineRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public string Reason { get; set; } = "KHAC";
    public string? Note { get; set; }
    public string Condition { get; set; } = "Bình thường";
    public string Handling { get; set; } = "NHAP_LAI";
}

public sealed record ExchangePreview(
    long InvoiceId,
    long ReturnValue,
    long NewOrderTotal,
    long Difference,
    IReadOnlyList<ReturnPreviewLine> ReturnedLines,
    bool CanComplete,
    string? Error);

public sealed class ExchangePage
{
    public ReturnForm? Invoice { get; set; }
    public ExchangeRequest Request { get; set; } = new();
    public ExchangePreview? Preview { get; set; }
    public string? Error { get; set; }
}

public sealed class ReturnOrderRequest
{
    public long InvoiceId { get; set; }
    public List<ReturnLineRequest> Lines { get; set; } = [];
    public string? ConfirmationToken { get; set; }
    public bool CashConfirmed { get; set; }
}

public sealed record ReturnFormLine(
    int ProductId,
    string Name,
    string Unit,
    long UnitPaid,
    int Purchased,
    long Remaining,
    bool Forbidden,
    bool Discontinued = false);

public sealed record ReturnForm(
    long InvoiceId,
    string InvoiceCode,
    DateTimeOffset PurchasedAt,
    IReadOnlyList<ReturnFormLine> Lines);

public sealed record ReturnPreviewLine(
    int ProductId,
    string Name,
    int Quantity,
    long UnitPaid,
    string Reason,
    string? Note,
    string Condition,
    string Handling,
    bool Exception,
    [property: JsonIgnore] decimal Cost,
    bool Discontinued = false,
    decimal ScrapValue = 0m);

public sealed record ReturnPreview(
    long InvoiceId,
    string InvoiceCode,
    long Refund,
    bool Expired,
    bool Forbidden,
    bool OverThreshold,
    bool NeedsApproval,
    bool RequiresPointsRecovery,
    string ConfirmationToken,
    IReadOnlyList<ReturnPreviewLine> Lines);

public sealed record ReturnReceipt(
    long Id,
    string Code,
    long InvoiceId,
    string InvoiceCode,
    string Status,
    long Refund,
    int PointsRecovered,
    DateTimeOffset CreatedAt,
    string Cashier,
    IReadOnlyList<ReturnPreviewLine> Lines,
    string Type = "TRA_HANG",  // "TRA_HANG" or "DOI_HANG"
    long? NewInvoiceId = null,  // DOI_HANG only
    string? NewInvoiceCode = null,  // DOI_HANG only
    long ExtraPaid = 0)  // DOI_HANG only: tiền khách trả thêm (tien_thu_them)
{
}

public sealed class ReturnOrderPage
{
    public ReturnForm? Invoice { get; set; }
    public ReturnOrderRequest Request { get; set; } = new();
    public ReturnPreview? Preview { get; set; }
    public string? Error { get; set; }
}
