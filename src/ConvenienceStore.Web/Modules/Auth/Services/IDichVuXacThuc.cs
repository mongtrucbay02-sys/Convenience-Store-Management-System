using ConvenienceStore.Web.Modules.Auth.Dtos;

namespace ConvenienceStore.Web.Modules.Auth.Services;

public interface IDichVuXacThuc
{
    Task<KetQuaDangNhapNoiBo> DangNhapAsync(YeuCauDangNhap yeuCau, CancellationToken ct = default);

    Task DangXuatAsync(long phienId, CancellationToken ct = default);

    Task DoiMatKhauAsync(int taiKhoanId, YeuCauDoiMatKhau yeuCau, CancellationToken ct = default);

    Task<KetQuaXacThucPhien> XacThucPhienAsync(string tokenPhien, CancellationToken ct = default);
}
