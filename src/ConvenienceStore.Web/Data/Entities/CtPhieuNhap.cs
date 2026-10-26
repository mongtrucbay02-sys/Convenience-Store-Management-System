using System;
using System.Collections.Generic;

namespace ConvenienceStore.Web.Data.Entities;

public partial class CtPhieuNhap
{
    public uint PhieuNhapId { get; set; }

    public uint SanPhamId { get; set; }

    public string TenSanPham { get; set; } = null!;

    public string DonViTinh { get; set; } = null!;

    public uint SoLuong { get; set; }

    public uint GiaNhap { get; set; }

    public ulong? ThanhTien { get; set; }

    public virtual PhieuNhap PhieuNhap { get; set; } = null!;

    public virtual SanPham SanPham { get; set; } = null!;
}
