using System;
using System.Collections.Generic;

namespace ConvenienceStore.Web.Data.Entities;

public partial class PhieuDoiTra
{
    public uint Id { get; set; }

    public string MaPhieu { get; set; } = null!;

    public string Loai { get; set; } = null!;

    public uint HoaDonGocId { get; set; }

    public uint? HoaDonMoiId { get; set; }

    public uint? KhachHangId { get; set; }

    public uint ChinhSachId { get; set; }

    public string TrangThai { get; set; } = null!;

    public bool CanDuyetQuaHan { get; set; }

    public bool CanDuyetDanhMuc { get; set; }

    public bool CanDuyetVuotNguong { get; set; }

    public ulong GiaTriHangTra { get; set; }

    public ulong TienHoan { get; set; }

    public ulong TienThuThem { get; set; }

    public uint DiemThuHoi { get; set; }

    public uint ThuNganId { get; set; }

    public DateTime NgayLap { get; set; }

    public uint? NguoiDuyetId { get; set; }

    public DateTime? DuyetLuc { get; set; }

    public string? GhiChuDuyet { get; set; }

    public DateTime? HoanTatLuc { get; set; }

    public virtual ChinhSachDoiTra ChinhSach { get; set; } = null!;

    public virtual ICollection<CtPhieuDoiTra> CtPhieuDoiTraPhieuDoiTraNavigations { get; set; } = new List<CtPhieuDoiTra>();

    public virtual ICollection<CtPhieuDoiTra> CtPhieuDoiTraPhieuDoiTras { get; set; } = new List<CtPhieuDoiTra>();

    public virtual HoaDon HoaDonGoc { get; set; } = null!;

    public virtual HoaDon? HoaDonMoi { get; set; }

    public virtual KhachHang? KhachHang { get; set; }

    public virtual ICollection<LichSuDiem> LichSuDiems { get; set; } = new List<LichSuDiem>();

    public virtual TaiKhoan? NguoiDuyet { get; set; }

    public virtual TaiKhoan ThuNgan { get; set; } = null!;
}
