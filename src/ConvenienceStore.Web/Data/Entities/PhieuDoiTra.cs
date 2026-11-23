using System;
using System.Collections.Generic;

namespace ConvenienceStore.Web.Data.Entities;

public partial class PhieuDoiTra
{
    public long Id { get; set; }

    public string MaPhieu { get; set; } = null!;

    public string Loai { get; set; } = null!;

    public long HoaDonGocId { get; set; }

    public long? HoaDonMoiId { get; set; }

    public long? KhachHangId { get; set; }

    public long ChinhSachId { get; set; }

    public string TrangThai { get; set; } = null!;

    public bool CanDuyetQuaHan { get; set; }

    public bool CanDuyetDanhMuc { get; set; }

    public bool CanDuyetVuotNguong { get; set; }

    public long GiaTriHangTra { get; set; }

    public long TienHoan { get; set; }

    public long TienThuThem { get; set; }

    public long DiemThuHoi { get; set; }

    public long ThuNganId { get; set; }

    public DateTime NgayLap { get; set; }

    public long? NguoiDuyetId { get; set; }

    public DateTime? DuyetLuc { get; set; }

    public string? GhiChuDuyet { get; set; }

    public DateTime? HoanTatLuc { get; set; }

    public virtual ChinhSachDoiTra ChinhSach { get; set; } = null!;

    public virtual ICollection<CtPhieuDoiTra> CtPhieuDoiTraPhieuDoiTraNavigations { get; set; } = new List<CtPhieuDoiTra>();

    public virtual ICollection<CtPhieuDoiTra> CtPhieuDoiTraPhieuDoiTras { get; set; } = new List<CtPhieuDoiTra>();

    public virtual ICollection<GiaoDichTienMat> GiaoDichTienMats { get; set; } = new List<GiaoDichTienMat>();

    public virtual HoaDon HoaDonGoc { get; set; } = null!;

    public virtual HoaDon? HoaDonMoi { get; set; }

    public virtual KhachHang? KhachHang { get; set; }

    public virtual ICollection<LichSuDiem> LichSuDiems { get; set; } = new List<LichSuDiem>();

    public virtual TaiKhoan? NguoiDuyet { get; set; }

    public virtual TaiKhoan ThuNgan { get; set; } = null!;
}
