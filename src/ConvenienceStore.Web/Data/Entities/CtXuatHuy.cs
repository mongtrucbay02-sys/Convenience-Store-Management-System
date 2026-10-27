using System;
using System.Collections.Generic;

namespace ConvenienceStore.Web.Data.Entities;

public partial class CtXuatHuy
{
    public uint PhieuXuatHuyId { get; set; }

    public uint SanPhamId { get; set; }

    public uint SoLuong { get; set; }

    public string LyDo { get; set; } = null!;

    public string? GhiChuLyDo { get; set; }

    public decimal? GiaVonLucDuyet { get; set; }

    public virtual PhieuXuatHuy PhieuXuatHuy { get; set; } = null!;

    public virtual SanPham SanPham { get; set; } = null!;
}
