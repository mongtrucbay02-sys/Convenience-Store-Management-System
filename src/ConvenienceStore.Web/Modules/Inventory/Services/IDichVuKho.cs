using ConvenienceStore.Web.Modules.Inventory.Models;

namespace ConvenienceStore.Web.Modules.Inventory.Services;

public interface IDichVuKho
{
    Task<KetQuaBienDong> GhiNhanBienDongAsync(YeuCauBienDong yeuCau, CancellationToken ct = default);

    Task KhoiTaoTonKhoAsync(long sanPhamId, int nguongToiThieu = 5, CancellationToken ct = default);

    Task<IReadOnlyList<TonKhoHienTai>> LayTonKhoAsync(IReadOnlyCollection<long> sanPhamIds, CancellationToken ct = default);

    Task<int> DemSanPhamDuoiNguongAsync(CancellationToken ct = default);
}
