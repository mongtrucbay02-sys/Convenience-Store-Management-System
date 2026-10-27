using ConvenienceStore.Web.Modules.Auth.Dtos;
using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Modules.Auth.Services;
using ConvenienceStore.Web.Shared.Api;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Modules.Auth.Controllers;

[ApiController]
[Route("api/v1/accounts")]
[Tags("AUTH – Quản trị tài khoản")]
[Produces("application/json")]
[Authorize(Policy = MaQuyen.QuanTriTaiKhoan)]
public sealed partial class AccountsController(IDichVuTaiKhoan dichVuTaiKhoan) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("AUTH-05 Xem danh sách tài khoản")]
    [ProducesResponseType<TrangDuLieu<TaiKhoanDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<PhanHoiLoi>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TrangDuLieu<TaiKhoanDto>>> XemDanhSach(
        [FromQuery] BoLocTaiKhoan boLoc, CancellationToken ct) =>
        Ok(await dichVuTaiKhoan.XemDanhSachAsync(boLoc, ct));

    [HttpPost]
    [EndpointSummary("AUTH-06 Tạo tài khoản (gồm AUTH-10 Gán vai trò)")]
    [ProducesResponseType<TaiKhoanDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<PhanHoiLoi>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<PhanHoiLoi>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TaiKhoanDto>> TaoTaiKhoan([FromBody] YeuCauTaoTaiKhoan yeuCau, CancellationToken ct)
    {
        var taiKhoan = await dichVuTaiKhoan.TaoAsync(yeuCau, ct);
        return CreatedAtAction(nameof(XemTaiKhoan), new { id = taiKhoan.Id }, taiKhoan);
    }

    [HttpGet("{id:long}")]
    [EndpointSummary("Xem một tài khoản (dùng cho AUTH-07 → AUTH-10)")]
    [ProducesResponseType<TaiKhoanDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<PhanHoiLoi>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaiKhoanDto>> XemTaiKhoan(long id, CancellationToken ct) =>
        Ok(await dichVuTaiKhoan.XemAsync(id, ct));

    [HttpPut("{id:long}/role")]
    [EndpointSummary("AUTH-10 Gán vai trò")]
    [ProducesResponseType<TaiKhoanDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<PhanHoiLoi>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<PhanHoiLoi>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<PhanHoiLoi>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TaiKhoanDto>> GanVaiTro(long id, [FromBody] YeuCauGanVaiTro yeuCau, CancellationToken ct) =>
        Ok(await dichVuTaiKhoan.GanVaiTroAsync(id, yeuCau, ct));
}
