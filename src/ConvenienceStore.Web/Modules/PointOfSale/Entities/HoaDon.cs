using ConvenienceStore.Web.Modules.Customers.Entities;
using ConvenienceStore.Web.Modules.Identity.Entities;

namespace ConvenienceStore.Web.Modules.PointOfSale.Entities;

public sealed class HoaDon
{
    public uint Id { get; set; }

    public string MaHoaDon { get; set; } = string.Empty;

    public uint GioHangId { get; set; }
    public GioHang GioHang { get; set; } = null!;

    public uint ThuNganId { get; set; }
    public TaiKhoan ThuNgan { get; set; } = null!;

    public uint? KhachHangId { get; set; }
    public KhachHang? KhachHang { get; set; }

    public DateTime ThanhToanLuc { get; set; }

    public ulong TongTienHang { get; set; }
    public ulong TongGiam { get; set; }
    public ulong TongThanhToan { get; set; }

    public ulong TienTruHangDoi { get; set; }
    public ulong TienKhachDua { get; set; }
    public ulong TienThua { get; set; }

    public uint? SoTienMotDiem { get; set; }
    public uint DiemCong { get; set; }

    public string TrangThai { get; set; } = "DA_THANH_TOAN";

    public uint? NguoiHuyId { get; set; }
    public TaiKhoan? NguoiHuy { get; set; }

    public DateTime? HuyLuc { get; set; }
    public string? LyDoHuy { get; set; }
}