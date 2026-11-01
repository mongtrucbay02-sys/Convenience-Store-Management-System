namespace ConvenienceStore.Web.Models.Pos;

// POS-10: Bộ lọc danh sách hóa đơn (đã được dịch vụ chuẩn hóa và áp quy tắc BR-POS-11).
public sealed class PosInvoiceListFilter
{
    public const int PageSize = 20;

    // Khoảng ngày thanh toán theo giờ Việt Nam, gồm cả ngày đầu và ngày cuối.
    public DateOnly From { get; set; }

    public DateOnly To { get; set; }

    // null = tất cả trạng thái; hoặc DA_THANH_TOAN / DA_HUY.
    public string? Status { get; set; }

    // null = tất cả thu ngân. Với người chỉ có quyền "của mình", dịch vụ luôn ép bằng ID của chính họ.
    public int? CashierId { get; set; }

    // Tìm theo một phần mã hóa đơn.
    public string? Code { get; set; }

    public string? Customer { get; set; }

    public int Page { get; set; } = 1;
}

// Một dòng trong danh sách hóa đơn: mã, thời gian, thu ngân, khách, tổng tiền, trạng thái.
public sealed class PosInvoiceListItem
{
    public long Id { get; set; }

    public string Code { get; set; } = "";

    public DateTime PaidAt { get; set; }

    public string CashierName { get; set; } = "";

    public string CustomerName { get; set; } = "";

    public long Total { get; set; }

    public string Status { get; set; } = "";

    public bool IsCancelled =>
        string.Equals(Status, "DA_HUY", StringComparison.OrdinalIgnoreCase);
}

// Kết quả truy vấn: dòng của trang hiện tại + tổng cộng trên TOÀN BỘ kết quả lọc.
public sealed record PosInvoiceListResult(
    IReadOnlyList<PosInvoiceListItem> Items,
    int TotalCount,
    long PaidRevenue,
    int CancelledCount,
    int Page,
    int TotalPages);

public sealed record PosCashierOption(int Id, string Name);

// Dữ liệu cho màn hình "Hóa đơn".
public sealed class PosInvoiceListViewModel
{
    public PosInvoiceListFilter Filter { get; set; } = new();

    public IReadOnlyList<PosInvoiceListItem> Items { get; set; } = [];

    public int TotalCount { get; set; }

    // Tổng tiền của hóa đơn ĐÃ THANH TOÁN trong kết quả lọc (hóa đơn đã hủy không tính).
    public long PaidRevenue { get; set; }

    public int CancelledCount { get; set; }

    public int Page { get; set; } = 1;

    public int TotalPages { get; set; } = 1;

    // Chỉ người có quyền xem tất cả mới được lọc theo thu ngân.
    public bool CanFilterCashier { get; set; }

    // Chỉ hiện nút "Xem" khi người dùng được mở màn hình in hóa đơn (quyền BAN_HANG).
    public bool CanOpenReceipt { get; set; }

    public IReadOnlyList<PosCashierOption> Cashiers { get; set; } = [];

    public string? Error { get; set; }
}
