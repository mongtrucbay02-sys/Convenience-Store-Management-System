using ConvenienceStore.Web.Modules.Auth.Dtos;
using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ConvenienceStore.Web.Modules.Auth.Controllers;

public sealed partial class AccountsController
{
    [HttpPut("{id:long}")]
    [EndpointSummary("AUTH-07 Sửa thông tin tài khoản")]
    [ProducesResponseType<TaiKhoanDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<PhanHoiLoi>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<PhanHoiLoi>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<PhanHoiLoi>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TaiKhoanDto>> SuaTaiKhoan(
        long id, [FromBody] YeuCauSuaTaiKhoan yeuCau, CancellationToken ct) =>
        Ok(await dichVuTaiKhoan.SuaAsync(id, yeuCau, ct));

    [HttpPost("{id:long}/lock")]
    [EndpointSummary("AUTH-08 Khóa tài khoản")]
    [ProducesResponseType<TaiKhoanDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<PhanHoiLoi>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<PhanHoiLoi>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<PhanHoiLoi>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TaiKhoanDto>> KhoaTaiKhoan(
        long id, [FromBody] YeuCauKhoaTaiKhoan yeuCau, CancellationToken ct) =>
        Ok(await dichVuTaiKhoan.KhoaAsync(id, yeuCau, User.LayTaiKhoanId(), ct));

    [HttpPost("{id:long}/unlock")]
    [EndpointSummary("AUTH-08 Mở khóa tài khoản")]
    [ProducesResponseType<TaiKhoanDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<PhanHoiLoi>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<PhanHoiLoi>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TaiKhoanDto>> MoKhoaTaiKhoan(long id, CancellationToken ct) =>
        Ok(await dichVuTaiKhoan.MoKhoaAsync(id, ct));

    [HttpPost("{id:long}/reset-password")]
    [EndpointSummary("AUTH-09 Đặt lại mật khẩu cho người dùng")]
    [ProducesResponseType<KetQuaDatLaiMatKhau>(StatusCodes.Status200OK)]
    [ProducesResponseType<PhanHoiLoi>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<PhanHoiLoi>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<PhanHoiLoi>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<KetQuaDatLaiMatKhau>> DatLaiMatKhau(
        long id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] YeuCauDatLaiMatKhau? yeuCau,
        CancellationToken ct)
    {
        var ketQua = await dichVuTaiKhoan.DatLaiMatKhauAsync(id, yeuCau, ct);
        Response.Headers.CacheControl = "no-store";
        return Ok(ketQua);
    }
}
