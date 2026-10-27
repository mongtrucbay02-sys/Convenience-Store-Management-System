namespace ConvenienceStore.Web.Modules.Inventory.Models;

public sealed record DongBienDong(
    int SanPhamId,
    int? SoLuong = null,
    int? SoLuongThucTe = null,
    decimal? GiaVon = null);
