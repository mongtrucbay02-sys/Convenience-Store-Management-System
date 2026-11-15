using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Modules.Products.Models;
using ConvenienceStore.Web.Modules.Products.Services;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Modules.Products.Controllers;

[Authorize(Policy = MaQuyen.SanPhamSua)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ProductEditingPageController(ProductEditService service, ProductDetailService details, ICategoryService categories, ProductImageService images) : Controller
{
    private const string Template = "~/Modules/Products/Views/Management/Edit.cshtml";
    [HttpGet("/Products/{id}/Edit")]
    public async Task<IActionResult> Edit(uint id, CancellationToken ct)
    {
        if (User.DangPhaiDoiMatKhau()) return Forbid();
        try {
            var row = await details.FindAsync(id, User.LayMaVaiTro(), ct); await Options(ct);
            return View(Template, new ProductEditModel { Id = id, TenSanPham = row.TenSanPham, DanhMucId = row.DanhMucId, DonViTinh = row.DonViTinh, MoTa = row.MoTa, NguongToiThieu = row.NguongToiThieu, PhienBan = row.PhienBan });
        }
        catch (LoiNghiepVu ex) when (ex.MaHttp == 404) { return NotFound(ex.Message); }
    }

    [HttpPost("/Products/{id}/Edit"), ValidateAntiForgeryToken, RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<IActionResult> Edit(uint id, [Bind("TenSanPham,DanhMucId,DonViTinh,MoTa,NguongToiThieu,PhienBan,Anh")] ProductEditModel model, CancellationToken ct)
    {
        if (User.DangPhaiDoiMatKhau()) return Forbid(); model.Id = id;
        if (!ModelState.IsValid) Response.StatusCode = 422;
        else {
            string? imageUrl = null;
            try {
                var input = ProductEditService.Normalize(model.ToRequest()); if (model.Anh is not null) imageUrl = await images.StoreAsync(model.Anh, ct);
                await service.ApplyAsync(id, input, User.LayMaVaiTro(), ct, imageUrl); return Redirect("/Products/" + id);
            }
            catch (LoiNghiepVu ex) when (ex.MaHttp is 422 or 409 or 404) {
                if (imageUrl is not null) images.Remove(imageUrl); Response.StatusCode = ex.MaHttp;
                if (ex.ChiTiet is IEnumerable<LoiTruong> errors) foreach (var error in errors) ModelState.AddModelError(error.Truong, error.Loi);
                else ModelState.AddModelError(string.Empty, ex.Message);
            }
        }
        await Options(ct); return View(Template, model);
    }
    private async Task Options(CancellationToken ct) => ViewData["Categories"] = (await categories.GetCategoriesAsync(new() { Size = 100 }, ct)).Items;
}
