using System;
using System.Collections.Generic;

namespace ConvenienceStore.Web.Data.Entities;

public partial class CtPhieuDoiTra
{
    public long PhieuDoiTraId { get; set; }

    public long SanPhamId { get; set; }

    public long HoaDonId { get; set; }

    public long SoLuong { get; set; }

    public long DonGiaThucTra { get; set; }

    public string LyDo { get; set; } = null!;

    public string? GhiChuLyDo { get; set; }

    public string? TinhTrang { get; set; }

    public string CachXuLy { get; set; } = null!;

    public bool NgoaiLe { get; set; }

    public decimal GiaVon { get; set; }

    public long? ThanhTien { get; set; }

    public decimal? GiaTriHuy { get; set; }

    public virtual CtHoaDon CtHoaDon { get; set; } = null!;

    public virtual PhieuDoiTra PhieuDoiTra { get; set; } = null!;

    public virtual PhieuDoiTra PhieuDoiTraNavigation { get; set; } = null!;
}
