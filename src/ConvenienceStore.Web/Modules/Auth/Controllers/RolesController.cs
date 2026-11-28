using ConvenienceStore.Web.Modules.Auth.Dtos;
using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Modules.Auth.Services;
using ConvenienceStore.Web.Shared.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Modules.Auth.Controllers;

[ApiController]
[Route("api/v1/roles")]
[Tags("AUTH – Quản trị tài khoản")]
[Produces("application/json")]
[Authorize(Policy = MaQuyen.QuanTriTaiKhoan)]
public sealed class RolesController(IDichVuTaiKhoan dichVuTaiKhoan) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("AUTH-10 Xem vai trò và quyền cài sẵn")]
    [ProducesResponseType<TrangDuLieu<VaiTroDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TrangDuLieu<VaiTroDto>>> XemDanhSachVaiTro(CancellationToken ct) =>
        Ok(await dichVuTaiKhoan.XemDanhSachVaiTroAsync(ct));
}
