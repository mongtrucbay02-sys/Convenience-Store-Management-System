using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Modules.Products.Models;
using ConvenienceStore.Web.Modules.Products.Services;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Modules.Products.Controllers;

[Authorize(Policy = MaQuyen.SanPhamGiaDanhMuc)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ProductPricingPageController(ProductPricingService service, ProductDetailService details) : Controller
{
    private const string Template = "~/Modules/Products/Views/Management/Price.cshtml";
    private const string CancelTemplate = "~/Modules/Products/Views/Management/CancelPrice.cshtml";
    [HttpGet("/Products/{id}/Price")]
    public async Task<IActionResult> Edit(uint id, CancellationToken ct)
    {
        if (User.DangPhaiDoiMatKhau()) return Forbid();
        try { var row = await details.FindAsync(id, User.LayMaVaiTro(), ct); ViewData["Product"] = row; return View(Template, new ProductPriceModel { Id = id, PhienBan = row.PhienBan }); }
        catch (LoiNghiepVu ex) when (ex.MaHttp == 404) { return NotFound(ex.Message); }
    }
    [HttpPost("/Products/{id}/Price"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(uint id, [Bind("GiaBan,HieuLucTu,PhienBan,XacNhanDuoiGiaVon")] ProductPriceModel model, CancellationToken ct)
    {
        if (User.DangPhaiDoiMatKhau()) return Forbid(); model.Id = id;
        if (!ModelState.IsValid) Response.StatusCode = 422;
        else try {
            await service.CreateAsync(id, model.ToRequest(), checked((uint)User.LayTaiKhoanId()), User.LayMaVaiTro(), ct); return Redirect($"/Products/{id}/Price");
        }
        catch (LoiNghiepVu ex) when (ex.MaHttp is 422 or 409 or 404) {
            Response.StatusCode = ex.MaHttp;
            if (ex.ChiTiet is IEnumerable<LoiTruong> errors) foreach(var error in errors) ModelState.AddModelError(error.Truong, error.Loi);
            else ModelState.AddModelError(string.Empty, ex.Message);
        }
        try { ViewData["Product"] = await details.FindAsync(id, User.LayMaVaiTro(), ct); }
        catch (LoiNghiepVu ex) when (ex.MaHttp == 404) { return NotFound(ex.Message); }
        return View(Template, model);
    }
    [HttpGet("/Products/{id}/Prices/{priceId}/Cancel")]
    public async Task<IActionResult> Cancel(uint id, uint priceId, CancellationToken ct)
    {
        if (User.DangPhaiDoiMatKhau()) return Forbid();
        try { return View(CancelTemplate, await service.FindAsync(id, priceId, User.LayMaVaiTro(), ct)); }
        catch (LoiNghiepVu ex) when (ex.MaHttp == 404) { return NotFound(ex.Message); }
    }
    [HttpPost("/Products/{id}/Prices/{priceId}/Cancel"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(uint id, uint priceId, [Bind("PhienBan")] ProductPriceCancelDto input, CancellationToken ct)
    {
        if (User.DangPhaiDoiMatKhau()) return Forbid();
        if (!ModelState.IsValid) Response.StatusCode = 422;
        else try { await service.CancelAsync(id, priceId, input, checked((uint)User.LayTaiKhoanId()), User.LayMaVaiTro(), ct); return Redirect($"/Products/{id}/Price"); }
        catch (LoiNghiepVu ex) when (ex.MaHttp is 422 or 409 or 404) { Response.StatusCode = ex.MaHttp; ModelState.AddModelError(string.Empty, ex.Message); }
        try { return View(CancelTemplate, (await service.FindAsync(id, priceId, User.LayMaVaiTro(), ct)) with { PhienBan = input.PhienBan }); }
        catch (LoiNghiepVu ex) when (ex.MaHttp == 404) { return NotFound(ex.Message); }
    }
}
