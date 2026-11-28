namespace ConvenienceStore.Web.Modules.Products.DTOs;

public sealed record ProductCategoryDto(long Id, string Ten);

public record ProductListItemDto
{
    public long Id { get; init; }
    public required string Ma { get; init; }
    public required string TenSanPham { get; init; }
    public long DanhMucId { get; init; }
    public required ProductCategoryDto DanhMuc { get; init; }
    public required string DonViTinh { get; init; }
    public long? GiaBan { get; init; }
    public int TonKho { get; init; }
    public decimal? GiaVon { get; init; }
    public required string TrangThai { get; init; }
    public long PhienBan { get; init; }
}
