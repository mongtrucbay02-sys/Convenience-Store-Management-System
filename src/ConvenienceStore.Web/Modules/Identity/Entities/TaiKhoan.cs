namespace ConvenienceStore.Web.Modules.Identity.Entities;

public sealed class TaiKhoan
{
    public uint Id { get; set; }

    public string TenDangNhap { get; set; } = string.Empty;
    public string MatKhauBam { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;

    public string? SoDienThoai { get; set; }
    public string? Email { get; set; }

    public byte VaiTroId { get; set; }
    public VaiTro VaiTro { get; set; } = null!;

    public string TrangThai { get; set; } = "HOAT_DONG";
    public string? LyDoKhoa { get; set; }

    public bool BatBuocDoiMk { get; set; } = true;
    public byte SoLanSai { get; set; }

    public DateTime? KhoaTamDen { get; set; }
    public DateTime? LanDangNhapCuoi { get; set; }

    public uint PhienBan { get; set; } = 1;

    public DateTime NgayTao { get; set; }
    public DateTime NgayCapNhat { get; set; }
}
