using ConvenienceStore.Web.Modules.Products.Entities;

namespace ConvenienceStore.Web.Modules.Inventory.Entities;

public sealed class CtKiemKe
{
    public uint PhieuKiemKeId { get; set; }
    public PhieuKiemKe PhieuKiemKe { get; set; } = null!;

    public uint SanPhamId { get; set; }
    public SanPham SanPham { get; set; } = null!;

    public int TonLucLap { get; set; }

    public int? SoLuongThucTe { get; set; }

    public string? LyDoChenhLech { get; set; }

    public int? ChenhLechLucDuyet { get; set; }

    public decimal? GiaVonLucDuyet { get; set; }
}