using ConvenienceStore.Web.Modules.Products.Entities;

namespace ConvenienceStore.Web.Modules.Purchasing.Entities;

public sealed class ChiTietPhieuNhap
{
    public uint PhieuNhapId { get; set; }

    public uint SanPhamId { get; set; }

    public string TenSanPham { get; set; } = string.Empty;

    public string DonViTinh { get; set; } = string.Empty;

    public uint SoLuong { get; set; }

    public uint GiaNhap { get; set; }

    public ulong ThanhTien { get; private set; }

    public PhieuNhap PhieuNhap { get; set; } = null!;

    public SanPham SanPham { get; set; } = null!;
}
