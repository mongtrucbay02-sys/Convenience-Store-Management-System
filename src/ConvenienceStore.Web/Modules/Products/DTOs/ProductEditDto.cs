namespace ConvenienceStore.Web.Modules.Products.DTOs;

public sealed record ProductEditDto
{
    public string? TenSanPham { get; init; }
    public uint DanhMucId { get; init; }
    public string? DonViTinh { get; init; }
    public string? MoTa { get; init; }
    public int NguongToiThieu { get; init; }
    public uint PhienBan { get; init; }
}
