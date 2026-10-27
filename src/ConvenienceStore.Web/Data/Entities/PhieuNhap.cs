using System;
using System.Collections.Generic;

namespace ConvenienceStore.Web.Data.Entities;

public partial class PhieuNhap
{
    public uint Id { get; set; }

    public string MaPhieu { get; set; } = null!;

    public uint NhaCungCapId { get; set; }

    public string? SoChungTuNcc { get; set; }

    public DateTime NgayNhap { get; set; }

    public string TrangThai { get; set; } = null!;

    public ulong TongTien { get; set; }

    public string? GhiChu { get; set; }

    public uint NguoiLapId { get; set; }

    public DateTime NgayLap { get; set; }

    public uint? NguoiXacNhanId { get; set; }

    public DateTime? XacNhanLuc { get; set; }

    public uint? NguoiHuyId { get; set; }

    public DateTime? HuyLuc { get; set; }

    public string? LyDoHuy { get; set; }

    public uint PhienBan { get; set; }

    public DateTime NgayCapNhat { get; set; }

    public virtual ICollection<CtPhieuNhap> CtPhieuNhaps { get; set; } = new List<CtPhieuNhap>();

    public virtual TaiKhoan? NguoiHuy { get; set; }

    public virtual TaiKhoan NguoiLap { get; set; } = null!;

    public virtual TaiKhoan? NguoiXacNhan { get; set; }

    public virtual NhaCungCap NhaCungCap { get; set; } = null!;
}
