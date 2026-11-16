namespace ConvenienceStore.Web.Modules.Products.DTOs;

// Compatible result contract from PR #202; current runtime preview is additive.
public enum CategoryRemovalStatus { Success, Invalid, NotFound, Conflict, Referenced }
public sealed record CategoryRemovalResult(CategoryRemovalStatus Status, CategoryRemovalPreviewDto? Preview = null);
public sealed record CategoryRemovalCommand { public uint PhienBan { get; init; } public bool XacNhan { get; init; } }
public sealed record CategoryRemovalPreviewDto(uint Id, string Ten, string TrangThai, uint PhienBan,
    int SoSanPham, int SoSanPhamDangKinhDoanh, bool CoPhieuKiemKe);
