using System;
using System.Collections.Generic;

namespace ConvenienceStore.Web.Data.Entities;

public partial class PhieuNhap
{
    public long Id { get; set; }

    public string MaPhieu { get; set; } = null!;

    public long NhaCungCapId { get; set; }

    public string? SoChungTuNcc { get; set; }

    public DateOnly NgayNhap { get; set; }

    public string TrangThai { get; set; } = null!;

    public long TongTien { get; set; }

    public string? GhiChu { get; set; }

    public long NguoiLapId { get; set; }

    public DateTime NgayLap { get; set; }

    public long? NguoiXacNhanId { get; set; }

    public DateTime? XacNhanLuc { get; set; }

    public long? NguoiHuyId { get; set; }

    public DateTime? HuyLuc { get; set; }

    public string? LyDoHuy { get; set; }

    public long PhienBan { get; set; }

    public DateTime NgayCapNhat { get; set; }

    public virtual ICollection<CtPhieuNhap> CtPhieuNhaps { get; set; } = new List<CtPhieuNhap>();

    public virtual TaiKhoan? NguoiHuy { get; set; }

    public virtual TaiKhoan NguoiLap { get; set; } = null!;

    public virtual TaiKhoan? NguoiXacNhan { get; set; }

    public virtual NhaCungCap NhaCungCap { get; set; } = null!;
}
