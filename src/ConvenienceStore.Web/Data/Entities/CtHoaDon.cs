using System;
using System.Collections.Generic;

namespace ConvenienceStore.Web.Data.Entities;

public partial class CtHoaDon
{
    public long HoaDonId { get; set; }

    public long SanPhamId { get; set; }

    public string TenSanPham { get; set; } = null!;

    public string DonViTinh { get; set; } = null!;

    public long SoLuong { get; set; }

    public long DonGia { get; set; }

    public long? KhuyenMaiId { get; set; }

    public long GiamDonVi { get; set; }

    public decimal GiaVon { get; set; }

    public long? ThanhTien { get; set; }

    public virtual ICollection<CtPhieuDoiTra> CtPhieuDoiTras { get; set; } = new List<CtPhieuDoiTra>();

    public virtual HoaDon HoaDon { get; set; } = null!;

    public virtual ChuongTrinhKhuyenMai? KhuyenMai { get; set; }

    public virtual SanPham SanPham { get; set; } = null!;
}
