using ConvenienceStore.Web.Modules.Products.DTOs;

namespace ConvenienceStore.Web.Modules.Products.Models;

public sealed class ProductEditModel
{
    public uint Id { get; set; }
    public string? TenSanPham { get; set; }
    public uint DanhMucId { get; set; }
    public string? DonViTinh { get; set; }
    public string? MoTa { get; set; }
    public int NguongToiThieu { get; set; }
    public uint PhienBan { get; set; }
    public IFormFile? Anh { get; set; }
    public ProductEditDto ToRequest() => new() { TenSanPham = TenSanPham, DanhMucId = DanhMucId, DonViTinh = DonViTinh, MoTa = MoTa, NguongToiThieu = NguongToiThieu, PhienBan = PhienBan };
}
