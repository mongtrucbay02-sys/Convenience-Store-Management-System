using ConvenienceStore.Web.Modules.Auth.Dtos;

namespace ConvenienceStore.Web.Modules.Auth.Services;

public partial interface IDichVuTaiKhoan
{
    Task<TaiKhoanDto> SuaAsync(long id, YeuCauSuaTaiKhoan yeuCau, CancellationToken ct = default);

    Task<TaiKhoanDto> KhoaAsync(long id, YeuCauKhoaTaiKhoan yeuCau, int nguoiThucHienId, CancellationToken ct = default);

    Task<TaiKhoanDto> MoKhoaAsync(long id, CancellationToken ct = default);

    Task<KetQuaDatLaiMatKhau> DatLaiMatKhauAsync(long id, YeuCauDatLaiMatKhau? yeuCau, CancellationToken ct = default);
}
