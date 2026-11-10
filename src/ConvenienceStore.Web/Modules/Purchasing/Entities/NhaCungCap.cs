namespace ConvenienceStore.Web.Modules.Purchasing.Entities;

public sealed class NhaCungCap
{
    public uint Id { get; set; }

    public string Ma { get; set; } = string.Empty;

    public string Ten { get; set; } = string.Empty;

    public string? NguoiLienHe { get; set; }

    public string SoDienThoai { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? DiaChi { get; set; }

    public string? MaSoThue { get; set; }

    public string? GhiChu { get; set; }

    public string TrangThai { get; set; } = "HOAT_DONG";

    public uint PhienBan { get; set; } = 1;

    public DateTime NgayTao { get; set; }

    public DateTime NgayCapNhat { get; set; }
}
