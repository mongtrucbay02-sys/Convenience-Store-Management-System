using ConvenienceStore.Web.Modules.Products.Entities;

namespace ConvenienceStore.Web.Modules.Promotions.Entities;

public sealed class KhuyenMaiSanPham
{
    public uint ChuongTrinhId { get; set; }

    public ChuongTrinhKhuyenMai ChuongTrinh { get; set; } = null!;

    public uint SanPhamId { get; set; }

    public SanPham SanPham { get; set; } = null!;
}