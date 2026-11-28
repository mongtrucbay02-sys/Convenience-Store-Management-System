using System;
using System.Collections.Generic;

namespace ConvenienceStore.Web.Data.Entities;

public partial class GioHang
{
    public long Id { get; set; }

    public long ThuNganId { get; set; }

    public long? KhachHangId { get; set; }

    public string TrangThai { get; set; } = null!;

    public string? GhiChuTamGiu { get; set; }

    public DateTime? TamGiuLuc { get; set; }

    public DateTime? HoanTatLuc { get; set; }

    public DateTime? HuyLuc { get; set; }

    public string? LyDoHuy { get; set; }

    public long? GiaTriKhiHuy { get; set; }

    public long PhienBan { get; set; }

    public DateTime NgayTao { get; set; }

    public DateTime NgayCapNhat { get; set; }

    public long? DangBanCuaThuNgan { get; set; }

    public virtual ICollection<CtGioHang> CtGioHangs { get; set; } = new List<CtGioHang>();

    public virtual HoaDon? HoaDon { get; set; }

    public virtual KhachHang? KhachHang { get; set; }

    public virtual TaiKhoan ThuNgan { get; set; } = null!;
}
