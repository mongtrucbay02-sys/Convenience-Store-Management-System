using ConvenienceStore.Web.Modules.Products.Entities;

namespace ConvenienceStore.Web.Modules.Inventory.Entities;

public sealed class CtXuatHuy
{
    public uint PhieuXuatHuyId { get; set; }
    public PhieuXuatHuy PhieuXuatHuy { get; set; } = null!;

    public uint SanPhamId { get; set; }
    public SanPham SanPham { get; set; } = null!;

    public uint SoLuong { get; set; }

    public string LyDo { get; set; } = string.Empty;

    public string? GhiChuLyDo { get; set; }

    public decimal? GiaVonLucDuyet { get; set; }
}