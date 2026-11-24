using System;
using System.Collections.Generic;

namespace ConvenienceStore.Web.Data.Entities;

public partial class CtKiemKe
{
    public long PhieuKiemKeId { get; set; }

    public long SanPhamId { get; set; }

    public int TonLucLap { get; set; }

    public int? SoLuongThucTe { get; set; }

    public string? LyDoChenhLech { get; set; }

    public int? ChenhLechLucDuyet { get; set; }

    public decimal? GiaVonLucDuyet { get; set; }

    public virtual PhieuKiemKe PhieuKiemKe { get; set; } = null!;

    public virtual SanPham SanPham { get; set; } = null!;
}
