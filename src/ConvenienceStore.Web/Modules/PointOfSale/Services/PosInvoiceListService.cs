using System.Security.Claims;
using ConvenienceStore.Web.Models.Pos;
using ConvenienceStore.Web.Modules.Auth.Security;

namespace ConvenienceStore.Web.Services.Pos;

// Interface đọc danh sách hóa đơn từ CSDL (triển khai: MySqlPosInvoiceListStore).
public interface IPosInvoiceListStore
{
    Task<PosInvoiceListResult> SearchAsync(
        PosInvoiceListFilter filter,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PosCashierOption>> GetCashiersAsync(
        CancellationToken cancellationToken = default);
}

// Người đang xem hóa đơn, lấy từ claim của phiên đăng nhập (KHÔNG lấy từ request).
public sealed record PosInvoiceViewer(
    int UserId,
    bool CanViewAll,
    bool CanViewOwn,
    bool CanOpenReceipt)
{
    public bool CanView => CanViewAll || CanViewOwn;
}

// BR-POS-11: Admin (HOA_DON_XEM_TAT_CA) xem hóa đơn của mọi thu ngân;
// thu ngân (HOA_DON_XEM_CUA_MINH) chỉ xem hóa đơn do chính mình lập.
public static class PosInvoiceAccess
{
    // Tên policy dùng cho [Authorize] ở InvoicesController.
    public const string PolicyXem = "HOA_DON_XEM";

    public static PosInvoiceViewer FromUser(ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new PosInvoiceViewer(
            user.LayTaiKhoanId(),
            user.HasClaim(ThongTinXacThuc.ClaimQuyen, MaQuyen.HoaDonXemTatCa),
            user.HasClaim(ThongTinXacThuc.ClaimQuyen, MaQuyen.HoaDonXemCuaMinh),
            user.HasClaim(ThongTinXacThuc.ClaimQuyen, MaQuyen.BanHang));
    }

    // Dùng làm điều kiện của policy: có ít nhất một trong hai quyền xem hóa đơn.
    public static bool CoQuyenXem(ClaimsPrincipal user) =>
        user.HasClaim(ThongTinXacThuc.ClaimQuyen, MaQuyen.HoaDonXemTatCa)
        || user.HasClaim(ThongTinXacThuc.ClaimQuyen, MaQuyen.HoaDonXemCuaMinh);

    // Có được xem MỘT hóa đơn cụ thể của thu ngân invoiceCashierId hay không.
    public static bool CanView(PosInvoiceViewer viewer, int invoiceCashierId)
    {
        ArgumentNullException.ThrowIfNull(viewer);

        return viewer.CanViewAll
            || (viewer.CanViewOwn && viewer.UserId == invoiceCashierId);
    }
}

// Tham số người dùng gửi lên (chưa tin cậy).
public sealed record PosInvoiceListRequest(
    DateOnly? FromDate,
    DateOnly? ToDate,
    string? Status,
    int? CashierId,
    string? Code,
    int Page = 1,
    string? Customer = null);

public sealed class PosInvoiceListService
{
    public const int MaxDays = 366;
    private const int MaxCodeLength = 20;

    private readonly IPosInvoiceListStore _store;

    public PosInvoiceListService(IPosInvoiceListStore store)
    {
        _store = store;
    }

    // POS-10: Hóa đơn trong ngày (mặc định hôm nay) theo bộ lọc, kèm tổng cộng.
    public async Task<PosInvoiceListViewModel> GetListAsync(
        PosInvoiceListRequest request,
        PosInvoiceViewer viewer,
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(viewer);

        if (!viewer.CanView)
        {
            throw new UnauthorizedAccessException(
                "Không có quyền xem hóa đơn.");
        }

        var from = request.FromDate ?? request.ToDate ?? today;
        var to = request.ToDate ?? request.FromDate ?? today;

        var code = request.Code?.Trim();
        if (code is { Length: > MaxCodeLength })
        {
            code = code[..MaxCodeLength];
        }

        var filter = new PosInvoiceListFilter
        {
            From = from,
            To = to,
            Status = request.Status is "DA_THANH_TOAN" or "DA_HUY"
                ? request.Status
                : null,

            // BR-POS-11: người chỉ có quyền "của mình" LUÔN bị ép lọc theo ID của chính họ,
            // dù request có gửi cashierId nào.
            CashierId = viewer.CanViewAll ? request.CashierId : viewer.UserId,

            Code = string.IsNullOrEmpty(code) ? null : code,
            Customer = string.IsNullOrWhiteSpace(request.Customer) ? null : request.Customer.Trim(),
            Page = request.Page
        };

        var model = new PosInvoiceListViewModel
        {
            Filter = filter,
            CanFilterCashier = viewer.CanViewAll,
            CanOpenReceipt = viewer.CanOpenReceipt,
            Cashiers = viewer.CanViewAll
                ? await _store.GetCashiersAsync(cancellationToken)
                : []
        };

        if (to == DateOnly.MaxValue)
        {
            model.Error = "Đến ngày phải nhỏ hơn 31/12/9999.";
            return model;
        }

        if (from > to)
        {
            model.Error = "Từ ngày không được sau đến ngày.";
            return model;
        }

        if (to.DayNumber - from.DayNumber + 1 > MaxDays)
        {
            model.Error = $"Chỉ xem tối đa {MaxDays} ngày mỗi lần.";
            return model;
        }

        var result = await _store.SearchAsync(filter, cancellationToken);

        model.Items = result.Items;
        model.TotalCount = result.TotalCount;
        model.PaidRevenue = result.PaidRevenue;
        model.CancelledCount = result.CancelledCount;
        model.Page = result.Page;
        model.TotalPages = result.TotalPages;
        filter.Page = result.Page;

        return model;
    }
}
