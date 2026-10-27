using System;
using System.Collections.Generic;

namespace ConvenienceStore.Web.Data.Entities;

public partial class HoaDon
{
    public uint Id { get; set; }

    public string MaHoaDon { get; set; } = null!;

    public uint GioHangId { get; set; }

    public uint ThuNganId { get; set; }

    public uint? KhachHangId { get; set; }

    public DateTime ThanhToanLuc { get; set; }

    public ulong TongTienHang { get; set; }

    public ulong TongGiam { get; set; }

    public ulong TongThanhToan { get; set; }

    public ulong TienTruHangDoi { get; set; }

    public ulong TienKhachDua { get; set; }

    public ulong TienThua { get; set; }

    public uint? SoTienMotDiem { get; set; }

    public uint DiemCong { get; set; }

    public string TrangThai { get; set; } = null!;

    public uint? NguoiHuyId { get; set; }

    public DateTime? HuyLuc { get; set; }

    public string? LyDoHuy { get; set; }

    public virtual ICollection<CtHoaDon> CtHoaDons { get; set; } = new List<CtHoaDon>();

    public virtual GioHang GioHang { get; set; } = null!;

    public virtual KhachHang? KhachHang { get; set; }

    public virtual ICollection<LichSuDiem> LichSuDiems { get; set; } = new List<LichSuDiem>();

    public virtual TaiKhoan? NguoiHuy { get; set; }

    public virtual ICollection<PhieuDoiTra> PhieuDoiTraHoaDonGocs { get; set; } = new List<PhieuDoiTra>();

    public virtual PhieuDoiTra? PhieuDoiTraHoaDonMoi { get; set; }

    public virtual TaiKhoan ThuNgan { get; set; } = null!;
}
