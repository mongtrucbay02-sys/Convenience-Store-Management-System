using ConvenienceStore.Web.Modules.Customers.Entities;
using ConvenienceStore.Web.Modules.Identity.Entities;
using ConvenienceStore.Web.Modules.PointOfSale.Entities;

namespace ConvenienceStore.Web.Modules.Returns.Entities;

public sealed class PhieuDoiTra
{
    public uint Id { get; set; }

    public string MaPhieu { get; set; } = string.Empty;
    public string Loai { get; set; } = string.Empty;

    public uint HoaDonGocId { get; set; }
    public HoaDon HoaDonGoc { get; set; } = null!;

    public uint? HoaDonMoiId { get; set; }
    public HoaDon? HoaDonMoi { get; set; }

    public uint? KhachHangId { get; set; }
    public KhachHang? KhachHang { get; set; }

    public uint ChinhSachId { get; set; }
    public ChinhSachDoiTra ChinhSach { get; set; } = null!;

    public string TrangThai { get; set; } = string.Empty;

    public bool CanDuyetQuaHan { get; set; }
    public bool CanDuyetDanhMuc { get; set; }
    public bool CanDuyetVuotNguong { get; set; }

    public ulong GiaTriHangTra { get; set; }
    public ulong TienHoan { get; set; }
    public ulong TienThuThem { get; set; }
    public uint DiemThuHoi { get; set; }

    public uint ThuNganId { get; set; }
    public TaiKhoan ThuNgan { get; set; } = null!;

    public DateTime NgayLap { get; set; }

    public uint? NguoiDuyetId { get; set; }
    public TaiKhoan? NguoiDuyet { get; set; }

    public DateTime? DuyetLuc { get; set; }
    public string? GhiChuDuyet { get; set; }
    public DateTime? HoanTatLuc { get; set; }
}