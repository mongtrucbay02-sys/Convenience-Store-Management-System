using System;
using System.Collections.Generic;

namespace ConvenienceStore.Web.Data.Entities;

public partial class CtPhieuDoiTra
{
    public uint PhieuDoiTraId { get; set; }

    public uint SanPhamId { get; set; }

    public uint HoaDonId { get; set; }

    public uint SoLuong { get; set; }

    public uint DonGiaThucTra { get; set; }

    public string LyDo { get; set; } = null!;

    public string? GhiChuLyDo { get; set; }

    public string? TinhTrang { get; set; }

    public string CachXuLy { get; set; } = null!;

    public bool NgoaiLe { get; set; }

    public decimal GiaVon { get; set; }

    public ulong? ThanhTien { get; set; }

    public decimal? GiaTriHuy { get; set; }

    public virtual CtHoaDon CtHoaDon { get; set; } = null!;

    public virtual PhieuDoiTra PhieuDoiTra { get; set; } = null!;

    public virtual PhieuDoiTra PhieuDoiTraNavigation { get; set; } = null!;
}
