using System.Globalization;
using System.Security.Claims;
using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Modules.Returns.Models;
using ConvenienceStore.Web.Shared.Api;
using ConvenienceStore.Web.Shared.Errors;

namespace ConvenienceStore.Web.Modules.Returns.Services;

public interface IInvoiceLookupStore
{
    Task<InvoiceLookupSnapshot> ReadAsync(InvoiceLookupFilter filter, DateTime nowVietnam,
        CancellationToken cancellationToken = default);
}

public static class ReturnLookupAccess
{
    public static void Require(ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);
        if (user.Identity?.IsAuthenticated != true)
            throw new LoiNghiepVu(MaLoiChung.ChuaDangNhap, 401, "Vui lòng đăng nhập.");
        if (!user.IsInRole(BangQuyen.ThuNgan)
            || !user.HasClaim(ThongTinXacThuc.ClaimQuyen, MaQuyen.DoiTra)
            || !int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), NumberStyles.None,
                CultureInfo.InvariantCulture, out var id) || id <= 0)
            throw new LoiNghiepVu(MaLoiChung.KhongDuQuyen, 403, "Bạn không có quyền tra cứu đổi trả.");
        if (user.DangPhaiDoiMatKhau())
            throw new LoiNghiepVu(MaLoiChung.PhaiDoiMatKhau, 403, "Vui lòng đổi mật khẩu trước khi tra cứu.");
    }
}

public sealed class InvoiceLookupService(IInvoiceLookupStore store, TimeProvider time)
{
    public async Task<TrangDuLieu<OriginalInvoiceDto>> LookupAsync(InvoiceLookupRequest request,
        ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        ReturnLookupAccess.Require(user);
        ArgumentNullException.ThrowIfNull(request);
        var ma = request.Ma?.Trim();
        var phone = request.SoDienThoai?.Trim();
        var hasCode = !string.IsNullOrEmpty(ma);
        var hasPhone = !string.IsNullOrEmpty(phone);
        if (hasCode == hasPhone)
            throw LoiNghiepVu.DuLieuKhongHopLe("ma", "Nhập mã hóa đơn hoặc số điện thoại, chỉ một trong hai.");
        if (hasCode && ma!.Length > 20)
            throw LoiNghiepVu.DuLieuKhongHopLe("ma", "Mã hóa đơn không được dài quá 20 ký tự.");
        if (hasPhone && (phone!.Length != 10 || phone[0] != '0' || phone.Any(c => c is < '0' or > '9')))
            throw LoiNghiepVu.DuLieuKhongHopLe("soDienThoai", "Số điện thoại phải gồm 10 chữ số, bắt đầu bằng 0.");
        if (request.Page < 1 || request.Size < 1)
            throw LoiNghiepVu.DuLieuKhongHopLe("page", "Trang và số dòng mỗi trang phải lớn hơn 0.");

        var filter = new InvoiceLookupFilter(hasCode ? ma : null, hasPhone ? phone : null,
            request.Page, Math.Min(request.Size, 100));
        var now = time.GetUtcNow().ToOffset(GioVietNam.DoLech).DateTime;
        var snapshot = await store.ReadAsync(filter, now, cancellationToken);
        var policy = snapshot.Policy ?? throw LoiNghiepVu.XungDot(MaLoiChung.XungDotDuLieu,
            "Chưa có chính sách đổi trả đang hiệu lực. Vui lòng liên hệ quản lý.");
        var items = snapshot.Items.Select(invoice => Map(invoice, policy, now)).ToArray();
        return new TrangDuLieu<OriginalInvoiceDto>(items, snapshot.Total, snapshot.Page, snapshot.Size);
    }

    private static OriginalInvoiceDto Map(OriginalInvoiceFact invoice, ReturnLookupPolicy policy, DateTime now)
    {
        var purchaseDate = DateOnly.FromDateTime(invoice.NgayMua);
        var deadline = purchaseDate.DayNumber > DateOnly.MaxValue.DayNumber - policy.SoNgayDoiTra
            ? DateOnly.MaxValue : purchaseDate.AddDays(policy.SoNgayDoiTra);
        var expired = DateOnly.FromDateTime(now) > deadline;
        var cancelled = invoice.TrangThai == "DA_HUY";
        var invalid = invoice.TrangThai is not ("DA_THANH_TOAN" or "DA_HUY") || invoice.NgayMua > now;
        var lines = invoice.ChiTiet.Select(line =>
        {
            var remaining = Math.Max(0L, (long)line.SoLuongMua - line.SoLuongDaTraHoacDangXuLy);
            var reasons = new List<string>();
            if (cancelled) reasons.Add("HOA_DON_DA_HUY");
            if (invalid) reasons.Add("HOA_DON_KHONG_HOP_LE");
            if (remaining == 0) reasons.Add("HET_SO_LUONG");
            if (expired) reasons.Add("QUA_HAN");
            if (line.KhongDoiTra) reasons.Add("DANH_MUC_KHONG_DOI_TRA");
            var approval = !cancelled && !invalid && remaining > 0 && (expired || line.KhongDoiTra);
            return new OriginalInvoiceLineDto(line.SanPhamId, line.TenSanPham, line.DonViTinh,
                line.TenDanhMuc, line.SoLuongMua, line.DonGia - line.GiamDonVi,
                line.SoLuongDaTraHoacDangXuLy, remaining, reasons.Count == 0, approval, reasons);
        }).ToArray();
        return new OriginalInvoiceDto
        {
            Id = invoice.Id, Ma = invoice.Ma, NgayMua = GioVietNam.ThanhOffset(invoice.NgayMua),
            TrangThai = invoice.TrangThai, TenThuNgan = invoice.TenThuNgan,
            TenKhachHang = string.IsNullOrWhiteSpace(invoice.TenKhachHang) ? "Khách lẻ" : invoice.TenKhachHang,
            SoDienThoai = MaskPhone(invoice.SoDienThoai), TongThanhToan = invoice.TongThanhToan,
            ChinhSachId = policy.Id, SoNgayDoiTra = policy.SoNgayDoiTra, NgayHetHan = deadline,
            DaHuy = cancelled, CoTheDoiTra = lines.Any(line => line.HopLe),
            CanDuyetNgoaiLe = lines.Any(line => line.CanDuyetNgoaiLe), ChiTiet = lines
        };
    }

    private static string? MaskPhone(string? phone) => string.IsNullOrEmpty(phone) ? null
        : phone.Length <= 4 ? new string('*', phone.Length) : new string('*', phone.Length - 4) + phone[^4..];
}
