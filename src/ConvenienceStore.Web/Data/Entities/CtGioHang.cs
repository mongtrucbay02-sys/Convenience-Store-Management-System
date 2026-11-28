using System;
using System.Collections.Generic;

namespace ConvenienceStore.Web.Data.Entities;

public partial class CtGioHang
{
    public long GioHangId { get; set; }

    public long SanPhamId { get; set; }

    public long SoLuong { get; set; }

    public long DonGia { get; set; }

    public long? KhuyenMaiId { get; set; }

    public long GiamDonVi { get; set; }

    public DateTime ThemLuc { get; set; }

    public virtual GioHang GioHang { get; set; } = null!;

    public virtual ChuongTrinhKhuyenMai? KhuyenMai { get; set; }

    public virtual SanPham SanPham { get; set; } = null!;
}
