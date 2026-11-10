using ConvenienceStore.Web.Modules.Identity.Entities;

namespace ConvenienceStore.Web.Modules.Promotions.Entities;

public sealed class ChuongTrinhKhuyenMai
{
    public uint Id { get; set; }

    public string Ma { get; set; } = null!;
    public string Ten { get; set; } = null!;
    public string? MoTa { get; set; }

    public string LoaiGiam { get; set; } = null!;
    public uint GiaTri { get; set; }
    public uint? GiamToiDa { get; set; }

    public DateTime BatDauLuc { get; set; }
    public DateTime KetThucLuc { get; set; }

    public bool ChiThanhVien { get; set; } = false;
    public string TrangThai { get; set; } = "NHAP";

    public uint NguoiTaoId { get; set; }
    public TaiKhoan NguoiTao { get; set; } = null!;

    public uint PhienBan { get; set; } = 1;

    public DateTime NgayTao { get; set; }
    public DateTime NgayCapNhat { get; set; }
}