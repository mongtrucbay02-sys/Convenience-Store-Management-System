
namespace ConvenienceStore.Web.Models.Pos;

// Thông tin một sản phẩm trên hóa đơn.
// Dữ liệu phải lấy từ snapshot lúc bán,
// không lấy lại tên hoặc giá hiện tại.
public sealed class PosInvoiceItemViewModel
{
    public string ProductName { get; set; } = "";

    public int Quantity { get; set; }

    public long UnitPrice { get; set; }

    public long Discount { get; set; }

    public long LineTotal { get; set; }
}

// Dữ liệu hóa đơn để hiển thị và in K80.
public sealed class PosInvoiceViewModel
{
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
