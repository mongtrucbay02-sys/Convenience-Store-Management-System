namespace ConvenienceStore.Web.Modules.Inventory.Models;

public sealed record DongBienDong(
    long SanPhamId,
    int? SoLuong = null,
    int? SoLuongThucTe = null,
    decimal? GiaVon = null);
