using System;
using System.Collections.Generic;

namespace ConvenienceStore.Web.Data.Entities;

public partial class CtGioHang
{
    public uint GioHangId { get; set; }

    public uint SanPhamId { get; set; }

    public uint SoLuong { get; set; }

    public uint DonGia { get; set; }

    public uint? KhuyenMaiId { get; set; }

    public uint GiamDonVi { get; set; }

    public DateTime ThemLuc { get; set; }

    public virtual GioHang GioHang { get; set; } = null!;

    public virtual ChuongTrinhKhuyenMai? KhuyenMai { get; set; }

    public virtual SanPham SanPham { get; set; } = null!;
}
