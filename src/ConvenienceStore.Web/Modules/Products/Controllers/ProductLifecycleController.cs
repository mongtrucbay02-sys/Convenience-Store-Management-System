using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Modules.Products.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Modules.Products.Controllers;

[ApiController, Route("api/v1/products/{id}")]
[Tags("SP")]
[Authorize(Policy = MaQuyen.SanPhamGiaDanhMuc)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ProductLifecycleController(ProductLifecycleService service) : ControllerBase
{
    /// <summary>SP-06 Preview stock, held/draft warnings and reference guards.</summary>
    [HttpGet("lifecycle")]
    public async Task<IActionResult> Preview(long id, CancellationToken ct) => Ok(await service.PreviewAsync(id, User.LayMaVaiTro(), ct));
    /// <summary>SP-06 Discontinue after confirmation, preserving historical records.</summary>
    [HttpPost("discontinue")]
    public async Task<IActionResult> Discontinue(long id, ProductLifecycleCommand input, CancellationToken ct) => Ok(await Execute(id, ProductLifecycleOperation.Discontinue, input, ct));
    /// <summary>SP-06 Reactivate a discontinued product.</summary>
    [HttpPost("reactivate")]
    public async Task<IActionResult> Reactivate(long id, ProductLifecycleCommand input, CancellationToken ct) => Ok(await Execute(id, ProductLifecycleOperation.Reactivate, input, ct));
    /// <summary>SP-06 Physically delete only an unused, zero-stock product.</summary>
    [HttpDelete]
    public async Task<IActionResult> Delete(long id, ProductLifecycleCommand input, CancellationToken ct) { await Execute(id, ProductLifecycleOperation.Delete, input, ct); return NoContent(); }
    private Task<ProductLifecyclePreview> Execute(long id, ProductLifecycleOperation operation, ProductLifecycleCommand input, CancellationToken ct) => service.ExecuteAsync(id, operation, input, checked((long)User.LayTaiKhoanId()), User.LayMaVaiTro(), ct);
}
