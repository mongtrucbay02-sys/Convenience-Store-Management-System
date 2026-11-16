using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Modules.Products.Services;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Modules.Products.Controllers;

[Authorize(Policy = MaQuyen.SanPhamGiaDanhMuc)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CategoryRemovalPageController(CategoryRemovalService service) : Controller
{
    [HttpGet("/Categories/{id}/Delete")]
    public Task<IActionResult> Delete(uint id, CancellationToken ct) => Render(id, false, null, ct);
    [HttpGet("/Categories/{id}/Hide")]
    public Task<IActionResult> Hide(uint id, CancellationToken ct) => Render(id, true, null, ct);
    [HttpPost("/Categories/{id}/Delete"), ValidateAntiForgeryToken]
    public Task<IActionResult> Delete(uint id, CategoryRemovalCommand input, CancellationToken ct) => Execute(id, false, input, ct);
    [HttpPost("/Categories/{id}/Hide"), ValidateAntiForgeryToken]
    public Task<IActionResult> Hide(uint id, CategoryRemovalCommand input, CancellationToken ct) => Execute(id, true, input, ct);
    private async Task<IActionResult> Execute(uint id, bool hide, CategoryRemovalCommand input, CancellationToken ct)
    {
        if (User.DangPhaiDoiMatKhau()) return Forbid();
        if (!ModelState.IsValid) Response.StatusCode = 422;
        else try { CategoryRemovalController.Require(hide ? await service.HideAsync(id, input, ct) : await service.DeleteAsync(id, input, ct), id); return Redirect("/Categories"); }
        catch (LoiNghiepVu ex) when (ex.MaHttp is 422 or 409 or 404) { Response.StatusCode = ex.MaHttp; ModelState.AddModelError(string.Empty, ex.Message); }
        return await Render(id, hide, input, ct);
    }
    private async Task<IActionResult> Render(uint id, bool hide, CategoryRemovalCommand? input, CancellationToken ct)
    {
        if (User.DangPhaiDoiMatKhau()) return Forbid(); var row = await service.PreviewAsync(id, ct); if (row is null) return NotFound("Danh mục không còn tồn tại.");
        ViewData["Hide"] = hide; ViewData["Command"] = input ?? new() { PhienBan = row.PhienBan };
        return View("~/Modules/Products/Views/Categories/Removal.cshtml", row);
    }
}
