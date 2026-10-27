namespace ConvenienceStore.Web.Modules.Auth.Dtos;

public sealed record NguoiDungDangNhap(
    long Id,
    string TenDangNhap,
    string HoTen,
    VaiTroTomTat VaiTro,
    IReadOnlyList<string> Quyen);
