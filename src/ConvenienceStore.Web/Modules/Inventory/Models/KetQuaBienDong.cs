namespace ConvenienceStore.Web.Modules.Inventory.Models;

public sealed record KetQuaBienDong(IReadOnlyList<KetQuaDong> Dong, int SoSanPhamDuoiNguong);
