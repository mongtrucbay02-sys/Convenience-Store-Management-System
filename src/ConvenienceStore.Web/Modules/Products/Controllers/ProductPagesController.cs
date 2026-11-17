using System.Data.Common;
using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Modules.Products.Services;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Modules.Products.Controllers;

[Authorize(Policy = MaQuyen.SanPhamXem)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ProductPagesController(ProductReadService service, ICategoryService categories, ILogger<ProductPagesController> logger) : Controller
{
    [HttpGet("/Products")]
    public async Task<IActionResult> Index([FromQuery] ProductListQuery query, CancellationToken ct)
    {
        if (User.DangPhaiDoiMatKhau()) return Forbid();
        ViewData["Query"] = query;
        try
        {
            if (!ModelState.IsValid) return Error(400, "Bộ lọc không đúng kiểu dữ liệu.");
            ProductListController.ValidateKeys(Request.Query.Keys);
            var result = await service.ListAsync(query, User.LayMaVaiTro(), ct);
            // Suggestions are bounded. Any valid category ID remains searchable beyond the first page.
            ViewData["Categories"] = (await categories.GetCategoriesAsync(new() { Size = 100 }, ct)).Items;
            return View("~/Modules/Products/Views/Management/Index.cshtml", result);
        }
        catch (LoiNghiepVu ex) { return Error(ex.MaHttp, ex.Message); }
        catch (DbException ex) { logger.LogError(ex, "Không tải được danh sách sản phẩm."); return Error(503, "Chưa tải được sản phẩm. Vui lòng thử lại."); }
    }

    private IActionResult Error(int status, string message)
    {
        Response.StatusCode = status; ViewData["ListError"] = message;
        return View("~/Modules/Products/Views/Management/Index.cshtml", new PagedResultDto<ProductListItemDto>([], 0, 1, 20));
    }
}
