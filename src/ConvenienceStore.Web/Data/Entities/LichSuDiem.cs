using System;
using System.Collections.Generic;

namespace ConvenienceStore.Web.Data.Entities;

public partial class LichSuDiem
{
    public long Id { get; set; }

    public long KhachHangId { get; set; }

    public string Loai { get; set; } = null!;

    public int SoDiem { get; set; }

    public long DiemSau { get; set; }

    public long? HoaDonId { get; set; }

    public long? PhieuDoiTraId { get; set; }

    public string? LyDo { get; set; }

    public long NguoiThucHienId { get; set; }

    public DateTime ThoiDiem { get; set; }

    public virtual HoaDon? HoaDon { get; set; }

    public virtual KhachHang KhachHang { get; set; } = null!;

    public virtual TaiKhoan NguoiThucHien { get; set; } = null!;

    public virtual PhieuDoiTra? PhieuDoiTra { get; set; }
}
