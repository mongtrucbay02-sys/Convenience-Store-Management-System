using System.Globalization;
using System.Security.Claims;

namespace ConvenienceStore.Web.Modules.Auth.Security;

public static class NguoiDungHienTai
{
    public static int LayTaiKhoanId(this ClaimsPrincipal nguoiDung) =>
        int.Parse(LayGiaTri(nguoiDung, ClaimTypes.NameIdentifier), CultureInfo.InvariantCulture);

    public static long LayPhienId(this ClaimsPrincipal nguoiDung) =>
        long.Parse(LayGiaTri(nguoiDung, ThongTinXacThuc.ClaimPhienId), CultureInfo.InvariantCulture);

    public static string LayMaVaiTro(this ClaimsPrincipal nguoiDung) => LayGiaTri(nguoiDung, ClaimTypes.Role);

    public static bool DangPhaiDoiMatKhau(this ClaimsPrincipal nguoiDung) =>
        nguoiDung.FindFirstValue(ThongTinXacThuc.ClaimBatBuocDoiMatKhau) == "true";

    private static string LayGiaTri(ClaimsPrincipal nguoiDung, string loai) =>
        nguoiDung.FindFirstValue(loai)
        ?? throw new InvalidOperationException($"Người dùng chưa đăng nhập hoặc thiếu thông tin {loai}.");
}
