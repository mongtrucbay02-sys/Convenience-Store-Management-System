using System.Globalization;
using System.Security.Claims;
using System.Text.Encodings.Web;
using ConvenienceStore.Web.Modules.Auth.Services;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ConvenienceStore.Web.Modules.Auth.Security;

public sealed class XuLyXacThucPhien(
    IOptionsMonitor<AuthenticationSchemeOptions> tuyChon,
    ILoggerFactory log,
    UrlEncoder maHoa,
    IDichVuXacThuc dichVuXacThuc)
    : AuthenticationHandler<AuthenticationSchemeOptions>(tuyChon, log, maHoa)
{
    private const string KhoaHetPhien = "XacThuc.HetPhien";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Cookies.TryGetValue(ThongTinXacThuc.CookiePhien, out var token) || string.IsNullOrEmpty(token))
            return AuthenticateResult.NoResult();

        var ketQua = await dichVuXacThuc.XacThucPhienAsync(token, Context.RequestAborted);
        if (ketQua.NguoiDung is not { } nguoiDung)
        {
            if (ketQua.HetPhien)
                Context.Items[KhoaHetPhien] = true;
            return AuthenticateResult.Fail("Phiên đăng nhập không hợp lệ hoặc đã hết hạn.");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, nguoiDung.TaiKhoanId.ToString(CultureInfo.InvariantCulture)),
            new(ClaimTypes.Name, nguoiDung.TenDangNhap),
            new(ClaimTypes.Role, nguoiDung.MaVaiTro),
            new(ThongTinXacThuc.ClaimHoTen, nguoiDung.HoTen),
            new(ThongTinXacThuc.ClaimPhienId, nguoiDung.PhienId.ToString(CultureInfo.InvariantCulture)),
            new(ThongTinXacThuc.ClaimBatBuocDoiMatKhau, nguoiDung.BatBuocDoiMatKhau ? "true" : "false"),
        };
        claims.AddRange(BangQuyen.LayQuyen(nguoiDung.MaVaiTro).Select(q => new Claim(ThongTinXacThuc.ClaimQuyen, q)));

        var danhTinh = new ClaimsIdentity(claims, Scheme.Name, ClaimTypes.Name, ClaimTypes.Role);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(danhTinh), Scheme.Name));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        var hetPhien = Context.Items.ContainsKey(KhoaHetPhien);
        if (hetPhien)
            CookieXacThuc.Xoa(Response, Request.IsHttps);
        return PhanHoiLoiApi.GhiAsync(
            Context,
            StatusCodes.Status401Unauthorized,
            hetPhien ? MaLoiChung.HetPhien : MaLoiChung.ChuaDangNhap,
            hetPhien ? ThongBaoLoi.HetPhien : ThongBaoLoi.ChuaDangNhap,
            null);
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties) =>
        PhanHoiLoiApi.GhiAsync(
            Context, StatusCodes.Status403Forbidden, MaLoiChung.KhongDuQuyen, ThongBaoLoi.KhongDuQuyen, null);
}
