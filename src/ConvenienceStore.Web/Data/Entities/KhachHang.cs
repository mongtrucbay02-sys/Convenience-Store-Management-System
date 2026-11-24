using System;
using System.Collections.Generic;

namespace ConvenienceStore.Web.Data.Entities;

public partial class KhachHang
{
    public long Id { get; set; }

    public string? HoTen { get; set; }

    public string? SoDienThoai { get; set; }

    public string? Email { get; set; }

    public DateOnly? NgaySinh { get; set; }

    public string? GioiTinh { get; set; }

    public long DiemHienCo { get; set; }

    public string TrangThai { get; set; } = null!;

    public DateTime? XoaDuLieuLuc { get; set; }

    public long NguoiTaoId { get; set; }

    public long PhienBan { get; set; }

    public DateTime NgayTao { get; set; }

    public DateTime NgayCapNhat { get; set; }

    public virtual ICollection<GioHang> GioHangs { get; set; } = new List<GioHang>();

    public virtual ICollection<HoaDon> HoaDons { get; set; } = new List<HoaDon>();

    public virtual ICollection<LichSuDiem> LichSuDiems { get; set; } = new List<LichSuDiem>();

    public virtual TaiKhoan NguoiTao { get; set; } = null!;

    public virtual ICollection<PhieuDoiTra> PhieuDoiTras { get; set; } = new List<PhieuDoiTra>();
}
