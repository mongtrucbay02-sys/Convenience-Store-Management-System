using System;
using System.Collections.Generic;

namespace ConvenienceStore.Web.Data.Entities;

public partial class SanPham
{
    public uint Id { get; set; }

    public string Ma { get; set; } = null!;

    public string TenSanPham { get; set; } = null!;

    public uint DanhMucId { get; set; }

    public string DonViTinh { get; set; } = null!;

    public string? AnhUrl { get; set; }

    public string? MoTa { get; set; }

    public string TrangThai { get; set; } = null!;

    public uint NguoiTaoId { get; set; }

    public uint PhienBan { get; set; }

    public DateTime NgayTao { get; set; }

    public DateTime NgayCapNhat { get; set; }

    public virtual ICollection<CtGioHang> CtGioHangs { get; set; } = new List<CtGioHang>();

    public virtual ICollection<CtHoaDon> CtHoaDons { get; set; } = new List<CtHoaDon>();

    public virtual ICollection<CtKiemKe> CtKiemKes { get; set; } = new List<CtKiemKe>();

    public virtual ICollection<CtPhieuNhap> CtPhieuNhaps { get; set; } = new List<CtPhieuNhap>();

    public virtual ICollection<CtXuatHuy> CtXuatHuys { get; set; } = new List<CtXuatHuy>();

    public virtual DanhMuc DanhMuc { get; set; } = null!;

    public virtual ICollection<LichSuGium> LichSuGia { get; set; } = new List<LichSuGium>();

    public virtual TaiKhoan NguoiTao { get; set; } = null!;

    public virtual ICollection<TheKho> TheKhos { get; set; } = new List<TheKho>();

    public virtual TonKho? TonKho { get; set; }

    public virtual ICollection<ChuongTrinhKhuyenMai> ChuongTrinhs { get; set; } = new List<ChuongTrinhKhuyenMai>();
}
