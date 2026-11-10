using ConvenienceStore.Web.Modules.Identity.Entities;
using ConvenienceStore.Web.Modules.Products.Entities;

namespace ConvenienceStore.Web.Modules.Inventory.Entities;

public sealed class PhieuKiemKe
{
    public uint Id { get; set; }

    public string MaPhieu { get; set; } = string.Empty;

    public string PhamVi { get; set; } = string.Empty;

    public uint? DanhMucId { get; set; }
    public DanhMuc? DanhMuc { get; set; }

    public string TrangThai { get; set; } = "NHAP";

    public string? GhiChu { get; set; }

    public uint NguoiLapId { get; set; }
    public TaiKhoan NguoiLap { get; set; } = null!;

    public DateTime NgayLap { get; set; }

    public DateTime? GuiDuyetLuc { get; set; }

    public uint? NguoiDuyetId { get; set; }
    public TaiKhoan? NguoiDuyet { get; set; }

    public DateTime? DuyetLuc { get; set; }

    public string? LyDoTuChoi { get; set; }

    public uint PhienBan { get; set; } = 1;
}