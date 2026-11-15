namespace ConvenienceStore.Web.Modules.Products.DTOs;

public sealed record ProductCategoryDto(uint Id, string Ten);

public sealed record ProductListItemDto
{
    public uint Id { get; init; }
    public required string Ma { get; init; }
    public required string TenSanPham { get; init; }
    public uint DanhMucId { get; init; }
    public required ProductCategoryDto DanhMuc { get; init; }
    public required string DonViTinh { get; init; }
    public uint? GiaBan { get; init; }
    public int TonKho { get; init; }
    public decimal? GiaVon { get; init; }
    public required string TrangThai { get; init; }
    public uint PhienBan { get; init; }
}
