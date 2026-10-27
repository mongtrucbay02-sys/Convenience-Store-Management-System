using System;
using System.Collections.Generic;

namespace ConvenienceStore.Web.Data.Entities;

public partial class TaiKhoan
{
    public uint Id { get; set; }

    public string TenDangNhap { get; set; } = null!;

    public string MatKhauBam { get; set; } = null!;

    public string HoTen { get; set; } = null!;

    public string? SoDienThoai { get; set; }

    public string? Email { get; set; }

    public byte VaiTroId { get; set; }

    public string TrangThai { get; set; } = null!;

    public string? LyDoKhoa { get; set; }

    public bool? BatBuocDoiMk { get; set; }

    public byte SoLanSai { get; set; }

    public DateTime? KhoaTamDen { get; set; }

    public DateTime? LanDangNhapCuoi { get; set; }

    public uint PhienBan { get; set; }

    public DateTime NgayTao { get; set; }

    public DateTime NgayCapNhat { get; set; }

    public virtual ICollection<ChinhSachDoiTra> ChinhSachDoiTras { get; set; } = new List<ChinhSachDoiTra>();

    public virtual ICollection<ChuongTrinhKhuyenMai> ChuongTrinhKhuyenMais { get; set; } = new List<ChuongTrinhKhuyenMai>();

    public virtual ICollection<GioHang> GioHangs { get; set; } = new List<GioHang>();

    public virtual ICollection<HoaDon> HoaDonNguoiHuys { get; set; } = new List<HoaDon>();

    public virtual ICollection<HoaDon> HoaDonThuNgans { get; set; } = new List<HoaDon>();

    public virtual ICollection<KhachHang> KhachHangs { get; set; } = new List<KhachHang>();

    public virtual ICollection<LichSuDiem> LichSuDiems { get; set; } = new List<LichSuDiem>();

    public virtual ICollection<LichSuGium> LichSuGiumNguoiHuys { get; set; } = new List<LichSuGium>();

    public virtual ICollection<LichSuGium> LichSuGiumNguoiTaos { get; set; } = new List<LichSuGium>();

    public virtual ICollection<PhienDangNhap> PhienDangNhaps { get; set; } = new List<PhienDangNhap>();

    public virtual ICollection<PhieuDoiTra> PhieuDoiTraNguoiDuyets { get; set; } = new List<PhieuDoiTra>();

    public virtual ICollection<PhieuDoiTra> PhieuDoiTraThuNgans { get; set; } = new List<PhieuDoiTra>();

    public virtual ICollection<PhieuKiemKe> PhieuKiemKeNguoiDuyets { get; set; } = new List<PhieuKiemKe>();

    public virtual ICollection<PhieuKiemKe> PhieuKiemKeNguoiLaps { get; set; } = new List<PhieuKiemKe>();

    public virtual ICollection<PhieuNhap> PhieuNhapNguoiHuys { get; set; } = new List<PhieuNhap>();

    public virtual ICollection<PhieuNhap> PhieuNhapNguoiLaps { get; set; } = new List<PhieuNhap>();

    public virtual ICollection<PhieuNhap> PhieuNhapNguoiXacNhans { get; set; } = new List<PhieuNhap>();

    public virtual ICollection<PhieuXuatHuy> PhieuXuatHuyNguoiDuyets { get; set; } = new List<PhieuXuatHuy>();

    public virtual ICollection<PhieuXuatHuy> PhieuXuatHuyNguoiLaps { get; set; } = new List<PhieuXuatHuy>();

    public virtual ICollection<QuyTacTichDiem> QuyTacTichDiems { get; set; } = new List<QuyTacTichDiem>();

    public virtual ICollection<SanPham> SanPhams { get; set; } = new List<SanPham>();

    public virtual ICollection<TheKho> TheKhos { get; set; } = new List<TheKho>();

    public virtual VaiTro VaiTro { get; set; } = null!;
}
