using System.Data.Common;
using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Modules.Products.Services;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Modules.Products.Controllers;

[Authorize(Policy = MaQuyen.SanPhamXem)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ProductDetailsPageController(ProductDetailService service, ILogger<ProductDetailsPageController> logger) : Controller
{
    [HttpGet("/Products/{id}")]
    public async Task<IActionResult> Details(uint id, CancellationToken ct)
    {
        if (User.DangPhaiDoiMatKhau()) return Forbid();
        try { return View("~/Modules/Products/Views/Management/Details.cshtml", await service.FindAsync(id, User.LayMaVaiTro(), ct)); }
        catch (LoiNghiepVu ex) when (ex.MaHttp == 404) { Response.StatusCode = 404; ViewData["DetailError"] = ex.Message; }
        catch (DbException ex) { logger.LogError(ex, "Không tải được chi tiết sản phẩm {ProductId}", id); Response.StatusCode = 503; ViewData["DetailError"] = "Chưa tải được sản phẩm. Vui lòng thử lại."; }
        return View("~/Modules/Products/Views/Management/Details.cshtml");
    }
}
