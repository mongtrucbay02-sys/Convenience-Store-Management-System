namespace ConvenienceStore.Web.Modules.Auth.Dtos;

public sealed record TaiKhoanDto(
    long Id,
    string TenDangNhap,
    string HoTen,
    string? SoDienThoai,
    string? Email,
    VaiTroTomTat VaiTro,
    string TrangThai,
    string? LyDoKhoa,
    DateTimeOffset? KhoaTamDen,
    bool BatBuocDoiMatKhau,
    DateTimeOffset? LanDangNhapCuoi,
    DateTimeOffset NgayTao,
    DateTimeOffset NgayCapNhat,
    int PhienBan);
