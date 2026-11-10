using ConvenienceStore.Web.Modules.Identity.Entities;
using ConvenienceStore.Web.Modules.PointOfSale.Entities;
using ConvenienceStore.Web.Modules.Returns.Entities;

namespace ConvenienceStore.Web.Modules.Customers.Entities;

public sealed class LichSuDiem
{
    public ulong Id { get; set; }

    public uint KhachHangId { get; set; }
    public KhachHang KhachHang { get; set; } = null!;

    public string Loai { get; set; } = string.Empty;

    public int SoDiem { get; set; }

    public uint DiemSau { get; set; }

    public uint? HoaDonId { get; set; }
    public HoaDon? HoaDon { get; set; }

    public uint? PhieuDoiTraId { get; set; }
    public PhieuDoiTra? PhieuDoiTra { get; set; }

    public string? LyDo { get; set; }

    public uint NguoiThucHienId { get; set; }
    public TaiKhoan NguoiThucHien { get; set; } = null!;

    public DateTime ThoiDiem { get; set; }
}
