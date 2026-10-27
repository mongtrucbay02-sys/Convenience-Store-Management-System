using System;
using System.Collections.Generic;

namespace ConvenienceStore.Web.Data.Entities;

public partial class LichSuDiem
{
    public ulong Id { get; set; }

    public uint KhachHangId { get; set; }

    public string Loai { get; set; } = null!;

    public int SoDiem { get; set; }

    public uint DiemSau { get; set; }

    public uint? HoaDonId { get; set; }

    public uint? PhieuDoiTraId { get; set; }

    public string? LyDo { get; set; }

    public uint NguoiThucHienId { get; set; }

    public DateTime ThoiDiem { get; set; }

    public virtual HoaDon? HoaDon { get; set; }

    public virtual KhachHang KhachHang { get; set; } = null!;

    public virtual TaiKhoan NguoiThucHien { get; set; } = null!;

    public virtual PhieuDoiTra? PhieuDoiTra { get; set; }
}
