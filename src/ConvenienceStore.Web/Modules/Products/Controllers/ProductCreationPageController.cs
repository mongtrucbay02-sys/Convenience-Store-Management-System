using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Modules.Products.Models;
using ConvenienceStore.Web.Modules.Products.Services;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Modules.Products.Controllers;

[Authorize(Policy = MaQuyen.SanPhamSua)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ProductCreationPageController(ProductCreationService service, ICategoryService categories, ProductImageService images) : Controller
{
    private const string Template = "~/Modules/Products/Views/Management/Create.cshtml";
    [HttpGet("/Products/Create")]
    public async Task<IActionResult> Create(bool compact = false, CancellationToken ct = default)
    {
        if (User.DangPhaiDoiMatKhau()) return Forbid();
        await Options(ct); return View(Template, new ProductCreateModel { Compact = compact });
    }

    [HttpPost("/Products/Create"), ValidateAntiForgeryToken, RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<IActionResult> Create([Bind("Ma,TenSanPham,DanhMucId,DonViTinh,GiaBan,NguongToiThieu,MoTa,Compact,Anh")] ProductCreateModel model, CancellationToken ct)
    {
        if (User.DangPhaiDoiMatKhau()) return Forbid();
        if (!ModelState.IsValid) Response.StatusCode = 422;
        else
        {
            string? imageUrl = null;
            try {
                var input = ProductCreationService.Normalize(model.ToRequest());
                if (model.Anh is not null) imageUrl = await images.StoreAsync(model.Anh, ct);
                var id = await service.CreateIdAsync(input, checked((uint)User.LayTaiKhoanId()), User.LayMaVaiTro(), ct, imageUrl); return Redirect("/Products/" + id);
            }
            catch (LoiNghiepVu ex) when (ex.MaHttp is 422 or 409)
            {
                if (imageUrl is not null) images.Remove(imageUrl);
                Response.StatusCode = ex.MaHttp;
                if (ex.ChiTiet is IEnumerable<LoiTruong> errors) foreach (var error in errors) ModelState.AddModelError(error.Truong, error.Loi);
                else ModelState.AddModelError(string.Empty, ex.Message);
            }
        }
        await Options(ct); return View(Template, model);
    }

    private async Task Options(CancellationToken ct) => ViewData["Categories"] = (await categories.GetCategoriesAsync(new() { Size = 100 }, ct)).Items.Where(x => x.TrangThai == "HOAT_DONG").ToArray();
}
