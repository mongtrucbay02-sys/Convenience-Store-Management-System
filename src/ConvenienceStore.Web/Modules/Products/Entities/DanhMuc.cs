namespace ConvenienceStore.Web.Modules.Products.Entities;

public sealed class DanhMuc
{
    public uint Id { get; set; }

    public string Ten { get; set; } = string.Empty;

    public string? MoTa { get; set; }

    public bool KhongDoiTra { get; set; }

    public string TrangThai { get; set; } = "HOAT_DONG";

    public uint PhienBan { get; set; } = 1;

    public DateTime NgayTao { get; set; }

    public DateTime NgayCapNhat { get; set; }
}
