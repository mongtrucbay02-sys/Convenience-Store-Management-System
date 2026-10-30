using ConvenienceStore.Web.Modules.Products.Entities;
using ConvenienceStore.Web.Modules.Promotions.Entities;

namespace ConvenienceStore.Web.Modules.PointOfSale.Entities;

public sealed class CtGioHang
{
    public uint GioHangId { get; set; }
    public GioHang GioHang { get; set; } = null!;

    public uint SanPhamId { get; set; }
    public SanPham SanPham { get; set; } = null!;

    public uint SoLuong { get; set; }

    public uint DonGia { get; set; }

    public uint? KhuyenMaiId { get; set; }
    public ChuongTrinhKhuyenMai? KhuyenMai { get; set; }

    public uint GiamDonVi { get; set; }

    public DateTime ThemLuc { get; set; }
}