
namespace ConvenienceStore.Web.Modules.Products.DTOs;

/// <summary>
/// DTO hiển thị thông tin danh mục cho SP-07.
/// Các trường đang ở trạng thái draft, chờ đối chiếu ERD/API.
/// </summary>
public sealed record CategoryListItemDto(
    uint Id,
    string Ten,
    bool KhongDoiTra,
    int SoSanPham,
    string TrangThai,
    uint PhienBan
);
