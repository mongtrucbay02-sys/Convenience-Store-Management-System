using ConvenienceStore.Web.Modules.Identity.Entities;
using ConvenienceStore.Web.Modules.Products.Entities;

namespace ConvenienceStore.Web.Modules.Inventory.Entities;

public sealed class TheKho
{
    public ulong Id { get; set; }

    public uint SanPhamId { get; set; }
    public SanPham SanPham { get; set; } = null!;

    public string LoaiBienDong { get; set; } = string.Empty;

    public string MaChungTu { get; set; } = string.Empty;

    public uint ChungTuId { get; set; }

    public int SoLuongThayDoi { get; set; }

    public int TonSau { get; set; }

    public decimal GiaVonApDung { get; set; }

    public decimal GiaVonSau { get; set; }

    public uint NguoiThucHienId { get; set; }
    public TaiKhoan NguoiThucHien { get; set; } = null!;

    public DateTime ThoiDiem { get; set; }
}