using ConvenienceStore.Web.Modules.Auth.Dtos;
using ConvenienceStore.Web.Shared.Api;

namespace ConvenienceStore.Web.Modules.Auth.Services;

public partial interface IDichVuTaiKhoan
{
    Task<TrangDuLieu<TaiKhoanDto>> XemDanhSachAsync(BoLocTaiKhoan boLoc, CancellationToken ct = default);

    Task<TaiKhoanDto> XemAsync(long id, CancellationToken ct = default);

    Task<TaiKhoanDto> TaoAsync(YeuCauTaoTaiKhoan yeuCau, CancellationToken ct = default);

    Task<TaiKhoanDto> GanVaiTroAsync(long id, YeuCauGanVaiTro yeuCau, CancellationToken ct = default);

    Task<TrangDuLieu<VaiTroDto>> XemDanhSachVaiTroAsync(CancellationToken ct = default);
}
