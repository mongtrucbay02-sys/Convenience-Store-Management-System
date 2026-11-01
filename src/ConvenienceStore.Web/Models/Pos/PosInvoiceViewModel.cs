
namespace ConvenienceStore.Web.Models.Pos;

// Thông tin một sản phẩm trên hóa đơn.
// Dữ liệu phải lấy từ snapshot lúc bán,
// không lấy lại tên hoặc giá hiện tại.
public sealed class PosInvoiceItemViewModel
{
    public int ProductId { get; set; }
    public string Unit { get; set; } = "";
    public int? PromotionId { get; set; }
    public string? PromotionCode { get; set; }
    public string ProductName { get; set; } = "";

    public int Quantity { get; set; }

    public long UnitPrice { get; set; }

    public long Discount { get; set; }

    public long LineTotal { get; set; }
}

// Dữ liệu hóa đơn để hiển thị và in K80.
public sealed class PosInvoiceViewModel
{
    public int? CustomerId { get; set; }
    public string? CustomerPhone { get; set; }
    public long ExchangeCredit { get; set; }
    public bool IsReprint { get; set; }
    public bool IsCancelled => Status == "DA_HUY";
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public string? CancelledBy { get; set; }
    public IReadOnlyList<PosInvoiceReturn> RelatedReturns { get; set; } = [];
    public long InvoiceId { get; set; }

    // BR-POS-11: đối chiếu tài khoản lập hóa đơn khi mở trực tiếp URL.
    public int CashierId { get; set; }

    public string InvoiceCode { get; set; } = "";

    public DateTime CreatedAt { get; set; }

    public string CashierName { get; set; } = "";

    public string? CustomerName { get; set; }

    public string Status { get; set; } = "";

    public List<PosInvoiceItemViewModel> Items { get; set; } = new();

    public long Subtotal { get; set; }

    public long Discount { get; set; }

    public long Total { get; set; }

    public long CashReceived { get; set; }

    public long Change { get; set; }

    // Điểm tích lũy từ hóa đơn này (0 nếu không có khách hoặc quy tắc đang tắt).
    public int PointsEarned { get; set; }
}

public sealed record PosInvoiceReturn(long Id, string Code, string Type, string Status,
    DateTime CreatedAt, long Refund, long AdditionalPayment, int PointsReversed, bool IsOriginalInvoice);
