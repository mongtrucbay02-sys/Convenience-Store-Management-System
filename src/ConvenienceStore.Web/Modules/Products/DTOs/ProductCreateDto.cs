namespace ConvenienceStore.Web.Modules.Products.DTOs;

public sealed record ProductCreateDto
{
    public string? Ma { get; init; }
    public string? TenSanPham { get; init; }
    public long DanhMucId { get; init; }
    public string? DonViTinh { get; init; }
    public long? GiaBan { get; init; }
    public int NguongToiThieu { get; init; } = 5;
    public string? MoTa { get; init; }
}
