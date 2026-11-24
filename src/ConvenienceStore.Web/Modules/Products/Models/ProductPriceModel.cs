using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Shared.Api;

namespace ConvenienceStore.Web.Modules.Products.Models;

public sealed class ProductPriceModel
{
    public long Id { get; set; }
    public long? GiaBan { get; set; }
    public DateTime? HieuLucTu { get; set; }
    public long PhienBan { get; set; }
    public bool XacNhanDuoiGiaVon { get; set; }
    public ProductPriceChangeDto ToRequest() => new() { GiaBan = GiaBan, PhienBan = PhienBan, XacNhanDuoiGiaVon = XacNhanDuoiGiaVon,
        HieuLucTu = HieuLucTu.HasValue ? GioVietNam.ThanhOffset(HieuLucTu.Value).ToString("O") : null };
}
