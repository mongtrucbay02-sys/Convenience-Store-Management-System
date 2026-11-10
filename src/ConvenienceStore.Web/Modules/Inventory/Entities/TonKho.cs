using ConvenienceStore.Web.Modules.Products.Entities;

namespace ConvenienceStore.Web.Modules.Inventory.Entities;

public sealed class TonKho
{
    public uint SanPhamId { get; set; }

    public SanPham SanPham { get; set; } = null!;

    public int SoLuongTon { get; set; }

    public decimal GiaVonBinhQuan { get; set; }

    public int NguongToiThieu { get; set; } = 5;

    public DateTime NgayCapNhat { get; set; }
}