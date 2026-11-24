using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Modules.Products.Services;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Modules.Products.Controllers;

[Authorize(Policy = MaQuyen.SanPhamGiaDanhMuc)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ProductLifecyclePageController(ProductLifecycleService service) : Controller
{
    [HttpGet("/Products/{id}/Discontinue")]
    public Task<IActionResult> Discontinue(long id, CancellationToken ct) => Render(id, ProductLifecycleOperation.Discontinue, null, ct);
    [HttpGet("/Products/{id}/Reactivate")]
    public Task<IActionResult> Reactivate(long id, CancellationToken ct) => Render(id, ProductLifecycleOperation.Reactivate, null, ct);
    [HttpGet("/Products/{id}/Delete")]
    public Task<IActionResult> Delete(long id, CancellationToken ct) => Render(id, ProductLifecycleOperation.Delete, null, ct);
    [HttpPost("/Products/{id}/Discontinue"), ValidateAntiForgeryToken]
    public Task<IActionResult> Discontinue(long id, ProductLifecycleCommand input, CancellationToken ct) => Execute(id, ProductLifecycleOperation.Discontinue, input, ct);
    [HttpPost("/Products/{id}/Reactivate"), ValidateAntiForgeryToken]
    public Task<IActionResult> Reactivate(long id, ProductLifecycleCommand input, CancellationToken ct) => Execute(id, ProductLifecycleOperation.Reactivate, input, ct);
    [HttpPost("/Products/{id}/Delete"), ValidateAntiForgeryToken]
    public Task<IActionResult> Delete(long id, ProductLifecycleCommand input, CancellationToken ct) => Execute(id, ProductLifecycleOperation.Delete, input, ct);
    private async Task<IActionResult> Execute(long id, ProductLifecycleOperation operation, ProductLifecycleCommand input, CancellationToken ct)
    {
        if (User.DangPhaiDoiMatKhau()) return Forbid();
        if (!ModelState.IsValid) Response.StatusCode = 422;
        else try { await service.ExecuteAsync(id, operation, input, checked((long)User.LayTaiKhoanId()), User.LayMaVaiTro(), ct); return Redirect(operation == ProductLifecycleOperation.Delete ? "/Products" : $"/Products/{id}"); }
        catch (LoiNghiepVu ex) when (ex.MaHttp is 422 or 409 or 404) { Response.StatusCode = ex.MaHttp; ModelState.AddModelError(string.Empty, ex.Message); }
        return await Render(id, operation, input, ct);
    }
    private async Task<IActionResult> Render(long id, ProductLifecycleOperation operation, ProductLifecycleCommand? input, CancellationToken ct)
    {
        if (User.DangPhaiDoiMatKhau()) return Forbid();
        try {
            var row = await service.PreviewAsync(id, User.LayMaVaiTro(), ct); ViewData["Operation"] = operation; ViewData["Command"] = input ?? new() { PhienBan = row.PhienBan };
            return View("~/Modules/Products/Views/Management/Lifecycle.cshtml", row);
        }
        catch (LoiNghiepVu ex) when (ex.MaHttp == 404) { return NotFound(ex.Message); }
    }
}
