using System;
using System.Collections.Generic;

namespace ConvenienceStore.Web.Data.Entities;

public partial class CtHoaDon
{
    public uint HoaDonId { get; set; }

    public uint SanPhamId { get; set; }

    public string TenSanPham { get; set; } = null!;

    public string DonViTinh { get; set; } = null!;

    public uint SoLuong { get; set; }

    public uint DonGia { get; set; }

    public uint? KhuyenMaiId { get; set; }

    public uint GiamDonVi { get; set; }

    public decimal GiaVon { get; set; }

    public ulong? ThanhTien { get; set; }

    public virtual ICollection<CtPhieuDoiTra> CtPhieuDoiTras { get; set; } = new List<CtPhieuDoiTra>();

    public virtual HoaDon HoaDon { get; set; } = null!;

    public virtual ChuongTrinhKhuyenMai? KhuyenMai { get; set; }

    public virtual SanPham SanPham { get; set; } = null!;
}
