using System;
using System.Collections.Generic;

namespace ConvenienceStore.Web.Data.Entities;

public partial class HoaDon
{
    public long Id { get; set; }

    public string MaHoaDon { get; set; } = null!;

    public long GioHangId { get; set; }

    public long ThuNganId { get; set; }

    public long? KhachHangId { get; set; }

    public DateTime ThanhToanLuc { get; set; }

    public long TongTienHang { get; set; }

    public long TongGiam { get; set; }

    public long TongThanhToan { get; set; }

    public long TienTruHangDoi { get; set; }

    public long TienKhachDua { get; set; }

    public long TienThua { get; set; }

    public long? SoTienMotDiem { get; set; }

    public long DiemCong { get; set; }

    public string TrangThai { get; set; } = null!;

    public long? NguoiHuyId { get; set; }

    public DateTime? HuyLuc { get; set; }

    public string? LyDoHuy { get; set; }

    public virtual ICollection<CtHoaDon> CtHoaDons { get; set; } = new List<CtHoaDon>();

    public virtual ICollection<GiaoDichTienMat> GiaoDichTienMats { get; set; } = new List<GiaoDichTienMat>();

    public virtual GioHang GioHang { get; set; } = null!;

    public virtual HoanTienHoaDon? HoanTienHoaDon { get; set; }

    public virtual KhachHang? KhachHang { get; set; }

    public virtual ICollection<LichSuDiem> LichSuDiems { get; set; } = new List<LichSuDiem>();

    public virtual TaiKhoan? NguoiHuy { get; set; }

    public virtual ICollection<PhieuDoiTra> PhieuDoiTraHoaDonGocs { get; set; } = new List<PhieuDoiTra>();

    public virtual PhieuDoiTra? PhieuDoiTraHoaDonMoi { get; set; }

    public virtual TaiKhoan ThuNgan { get; set; } = null!;
}
