namespace ConvenienceStore.Web.Modules.Auth.Services;

public sealed record NguoiDungPhien(
    long PhienId,
    int TaiKhoanId,
    string TenDangNhap,
    string HoTen,
    string MaVaiTro,
    bool BatBuocDoiMatKhau);
