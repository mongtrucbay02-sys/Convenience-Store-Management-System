namespace ConvenienceStore.Web.Modules.Auth.Services;

public sealed record NguoiDungPhien(
    long PhienId,
    long TaiKhoanId,
    string TenDangNhap,
    string HoTen,
    string MaVaiTro,
    bool BatBuocDoiMatKhau);
