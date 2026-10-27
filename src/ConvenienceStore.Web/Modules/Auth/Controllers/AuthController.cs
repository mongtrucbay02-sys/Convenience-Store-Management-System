using ConvenienceStore.Web.Modules.Auth.Dtos;
using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Modules.Auth.Services;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Modules.Auth.Controllers;

[ApiController]
[Route("api/v1/auth")]
[Tags("AUTH – Phiên đăng nhập")]
[Produces("application/json")]
public sealed class AuthController(IDichVuXacThuc dichVuXacThuc) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EndpointSummary("AUTH-01 Đăng nhập")]
    [ProducesResponseType<KetQuaDangNhap>(StatusCodes.Status200OK)]
    [ProducesResponseType<PhanHoiLoi>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<PhanHoiLoi>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<PhanHoiLoi>(StatusCodes.Status423Locked)]
    public async Task<ActionResult<KetQuaDangNhap>> DangNhap([FromBody] YeuCauDangNhap yeuCau, CancellationToken ct)
    {
        var ketQua = await dichVuXacThuc.DangNhapAsync(yeuCau, ct);
        CookieXacThuc.Dat(Response, ketQua.TokenPhien, Request.IsHttps);
        return Ok(ketQua.KetQua);
    }

    [HttpPost("logout")]
    [Authorize(Policy = MaQuyen.TaiKhoanCaNhan)]
    [ChoPhepKhiPhaiDoiMatKhau]
    [EndpointSummary("AUTH-02 Đăng xuất")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<PhanHoiLoi>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DangXuat(CancellationToken ct)
    {
        await dichVuXacThuc.DangXuatAsync(User.LayPhienId(), ct);
        CookieXacThuc.Xoa(Response, Request.IsHttps);
        return NoContent();
    }

    [HttpPost("change-password")]
    [Authorize(Policy = MaQuyen.TaiKhoanCaNhan)]
    [ChoPhepKhiPhaiDoiMatKhau]
    [EndpointSummary("AUTH-03 Đổi mật khẩu")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<PhanHoiLoi>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<PhanHoiLoi>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> DoiMatKhau([FromBody] YeuCauDoiMatKhau yeuCau, CancellationToken ct)
    {
        await dichVuXacThuc.DoiMatKhauAsync(User.LayTaiKhoanId(), yeuCau, ct);
        return NoContent();
    }
}
