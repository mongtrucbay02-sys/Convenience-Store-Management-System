using ConvenienceStore.Web.Modules.Identity.Entities;

namespace ConvenienceStore.Web.Modules.Customers.Entities;

public sealed class KhachHang
{
    public uint Id { get; set; }

    public string? HoTen { get; set; }

    public string? SoDienThoai { get; set; }

    public string? Email { get; set; }

    public DateTime? NgaySinh { get; set; }

    public string? GioiTinh { get; set; }

    public uint DiemHienCo { get; set; }

    public string TrangThai { get; set; } = "HOAT_DONG";

    public DateTime? XoaDuLieuLuc { get; set; }

    public uint NguoiTaoId { get; set; }
    public TaiKhoan NguoiTao { get; set; } = null!;

    public uint PhienBan { get; set; } = 1;

    public DateTime NgayTao { get; set; }

    public DateTime NgayCapNhat { get; set; }
}