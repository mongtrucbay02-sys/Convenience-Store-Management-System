using ConvenienceStore.Web.Modules.Inventory.Models;

namespace ConvenienceStore.Web.Modules.Inventory.Services;

public interface IDichVuKho
{
    Task<KetQuaBienDong> GhiNhanBienDongAsync(YeuCauBienDong yeuCau, CancellationToken ct = default);

    Task KhoiTaoTonKhoAsync(int sanPhamId, int nguongToiThieu = 5, CancellationToken ct = default);

    Task<IReadOnlyList<TonKhoHienTai>> LayTonKhoAsync(IReadOnlyCollection<int> sanPhamIds, CancellationToken ct = default);

    Task<int> DemSanPhamDuoiNguongAsync(CancellationToken ct = default);
}
