using ConvenienceStore.Web.Modules.Identity.Entities;

namespace ConvenienceStore.Web.Modules.Products.Entities;

public sealed class SanPham
{
    public uint Id { get; set; }

    public string Ma { get; set; } = string.Empty;

    public string TenSanPham { get; set; } = string.Empty;

    public uint DanhMucId { get; set; }
    public DanhMuc DanhMuc { get; set; } = null!;

    public string DonViTinh { get; set; } = string.Empty;

    public string? AnhUrl { get; set; }

    public string? MoTa { get; set; }

    public string TrangThai { get; set; } = "DANG_KINH_DOANH";

    public uint NguoiTaoId { get; set; }
    public TaiKhoan NguoiTao { get; set; } = null!;

    public uint PhienBan { get; set; } = 1;

    public DateTime NgayTao { get; set; }

    public DateTime NgayCapNhat { get; set; }
}
