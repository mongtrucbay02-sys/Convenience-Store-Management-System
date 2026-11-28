using ConvenienceStore.Web.Modules.Products.DTOs;

namespace ConvenienceStore.Web.Modules.Products.Models;

public sealed class ProductCreateModel
{
    public string? Ma { get; set; }
    public string? TenSanPham { get; set; }
    public long DanhMucId { get; set; }
    public string? DonViTinh { get; set; }
    public long? GiaBan { get; set; }
    public int NguongToiThieu { get; set; } = 5;
    public string? MoTa { get; set; }
    public bool Compact { get; set; }
    public IFormFile? Anh { get; set; }
    public ProductCreateDto ToRequest() => new() { Ma = Ma, TenSanPham = TenSanPham, DanhMucId = DanhMucId, DonViTinh = DonViTinh, GiaBan = GiaBan, NguongToiThieu = NguongToiThieu, MoTa = MoTa };
}
