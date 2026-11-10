using ConvenienceStore.Web.Modules.Identity.Entities;

namespace ConvenienceStore.Web.Modules.Purchasing.Entities;

public sealed class PhieuNhap
{
    public uint Id { get; set; }

    public string MaPhieu { get; set; } = string.Empty;

    public uint NhaCungCapId { get; set; }

    public string? SoChungTuNcc { get; set; }

    public DateOnly NgayNhap { get; set; }

    public string TrangThai { get; set; } = "NHAP";

    public ulong TongTien { get; set; }

    public string? GhiChu { get; set; }

    public uint NguoiLapId { get; set; }

    public DateTime NgayLap { get; set; }

    public uint? NguoiXacNhanId { get; set; }

    public DateTime? XacNhanLuc { get; set; }

    public uint? NguoiHuyId { get; set; }

    public DateTime? HuyLuc { get; set; }

    public string? LyDoHuy { get; set; }

    public uint PhienBan { get; set; } = 1;

    public DateTime NgayCapNhat { get; set; }

    public NhaCungCap NhaCungCap { get; set; } = null!;

    public TaiKhoan NguoiLap { get; set; } = null!;

    public TaiKhoan? NguoiXacNhan { get; set; }

    public TaiKhoan? NguoiHuy { get; set; }
}
