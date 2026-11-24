using System;
using System.Collections.Generic;

namespace ConvenienceStore.Web.Data.Entities;

public partial class GiaoDichTienMat
{
    public long Id { get; set; }

    public long QuyTienMatId { get; set; }

    public string Loai { get; set; } = null!;

    public long? HoaDonId { get; set; }

    public long? PhieuDoiTraId { get; set; }

    public long SoTien { get; set; }

    public long SoDuTruoc { get; set; }

    public long SoDuSau { get; set; }

    public long NguoiThucHienId { get; set; }

    public DateTime ThoiDiem { get; set; }

    public string? GhiChu { get; set; }

    public virtual HoaDon? HoaDon { get; set; }

    public virtual TaiKhoan NguoiThucHien { get; set; } = null!;

    public virtual PhieuDoiTra? PhieuDoiTra { get; set; }

    public virtual QuyTienMat QuyTienMat { get; set; } = null!;
}
