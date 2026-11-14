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
public sealed class ReturnOrderRequest
{
    public long InvoiceId { get; set; }
    public List<ReturnLineRequest> Lines { get; set; } = [];
    public string? ConfirmationToken { get; set; }
    public bool CashConfirmed { get; set; }
}
public sealed record ReturnFormLine(int ProductId, string Name, string Unit, long UnitPaid,
    int Purchased, long Remaining, bool Forbidden);
public sealed record ReturnForm(long InvoiceId, string InvoiceCode, DateTimeOffset PurchasedAt,
    IReadOnlyList<ReturnFormLine> Lines);
public sealed record ReturnPreviewLine(int ProductId, string Name, int Quantity, long UnitPaid,
    string Reason, string? Note, string Condition, string Handling, bool Exception, [property:JsonIgnore] decimal Cost);
public sealed record ReturnPreview(long InvoiceId, string InvoiceCode, long Refund,
    bool Expired, bool Forbidden, bool OverThreshold, bool NeedsApproval, bool PointsBlocked,
    string ConfirmationToken, IReadOnlyList<ReturnPreviewLine> Lines);
public sealed record ReturnReceipt(long Id, string Code, long InvoiceId, string InvoiceCode,
    string Status, long Refund, int PointsRecovered, DateTimeOffset CreatedAt, string Cashier,
    IReadOnlyList<ReturnPreviewLine> Lines);
public sealed class ReturnOrderPage
{
    public ReturnForm? Invoice { get; set; }
    public ReturnOrderRequest Request { get; set; } = new();
    public ReturnPreview? Preview { get; set; }
    public string? Error { get; set; }
}
