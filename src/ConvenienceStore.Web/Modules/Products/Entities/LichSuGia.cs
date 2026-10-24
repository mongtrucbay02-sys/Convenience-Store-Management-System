using ConvenienceStore.Web.Modules.Identity.Entities;

namespace ConvenienceStore.Web.Modules.Products.Entities;

public sealed class LichSuGia
{
    public uint Id { get; set; }

    public uint SanPhamId { get; set; }
    public SanPham SanPham { get; set; } = null!;

    public uint GiaBan { get; set; }

    public DateTime HieuLucTu { get; set; }

    public uint NguoiTaoId { get; set; }
    public TaiKhoan NguoiTao { get; set; } = null!;

    public DateTime NgayTao { get; set; }

    public DateTime? HuyLuc { get; set; }

    public uint? NguoiHuyId { get; set; }
    public TaiKhoan? NguoiHuy { get; set; }
}
