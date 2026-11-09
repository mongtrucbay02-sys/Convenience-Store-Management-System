using ConvenienceStore.Web.Shared.Api;

namespace ConvenienceStore.Web.Modules.Returns.Models;

public sealed class InvoiceLookupRequest
{
    public string? Ma { get; set; }
    public string? SoDienThoai { get; set; }
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 20;
}

public sealed record InvoiceLookupFilter(string? Ma, string? SoDienThoai, int Page, int Size);
public sealed record ReturnLookupPolicy(int Id, int SoNgayDoiTra);
public sealed record InvoiceLookupSnapshot(ReturnLookupPolicy? Policy,
    IReadOnlyList<OriginalInvoiceFact> Items, int Total, int Page, int Size);

public sealed class OriginalInvoiceFact
{
    public long Id { get; init; }
    public string Ma { get; init; } = "";
    public DateTime NgayMua { get; init; }
    public int ThuNganId { get; init; }
    public string TenThuNgan { get; init; } = "";
    public int? KhachHangId { get; init; }
    public string? TenKhachHang { get; init; }
    public string? SoDienThoai { get; init; }
    public long TongThanhToan { get; init; }
    public string TrangThai { get; init; } = "";
    public List<OriginalInvoiceLineFact> ChiTiet { get; } = [];
}

public sealed record OriginalInvoiceLineFact(int SanPhamId, string TenSanPham, string DonViTinh,
    int SoLuongMua, long DonGia, long GiamDonVi, string TenDanhMuc,
    bool KhongDoiTra, long SoLuongDaTraHoacDangXuLy);

public sealed class OriginalInvoiceDto
{
    public long Id { get; init; }
    public string Ma { get; init; } = "";
    public DateTimeOffset NgayMua { get; init; }
    public string TrangThai { get; init; } = "";
    public string TenThuNgan { get; init; } = "";
    public string TenKhachHang { get; init; } = "Khách lẻ";
    public string? SoDienThoai { get; init; }
    public long TongThanhToan { get; init; }
    public int ChinhSachId { get; init; }
    public int SoNgayDoiTra { get; init; }
    public DateOnly NgayHetHan { get; init; }
    public bool DaHuy { get; init; }
    public bool CoTheDoiTra { get; init; }
    public bool CanDuyetNgoaiLe { get; init; }
    public IReadOnlyList<OriginalInvoiceLineDto> ChiTiet { get; init; } = [];
}

public sealed record OriginalInvoiceLineDto(int SanPhamId, string TenSanPham, string DonViTinh,
    string TenDanhMuc, int SoLuongMua, long GiaThucTraMoiDonVi,
    long SoLuongDaTraHoacDangXuLy, long SoLuongConDuocTra,
    bool HopLe, bool CanDuyetNgoaiLe, IReadOnlyList<string> LyDo);

public sealed class InvoiceLookupViewModel
{
    public InvoiceLookupRequest Query { get; init; } = new();
    public TrangDuLieu<OriginalInvoiceDto>? Result { get; set; }
    public string? Error { get; set; }
}
