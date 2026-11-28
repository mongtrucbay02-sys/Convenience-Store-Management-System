using System;
using System.Collections.Generic;

namespace ConvenienceStore.Web.Data.Entities;

public partial class CtPhieuNhap
{
    public long PhieuNhapId { get; set; }

    public long SanPhamId { get; set; }

    public string TenSanPham { get; set; } = null!;

    public string DonViTinh { get; set; } = null!;

    public long SoLuong { get; set; }

    public long GiaNhap { get; set; }

    public long? ThanhTien { get; set; }

    public virtual PhieuNhap PhieuNhap { get; set; } = null!;

    public virtual SanPham SanPham { get; set; } = null!;
}
