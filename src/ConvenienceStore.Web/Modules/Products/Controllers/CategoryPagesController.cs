using System.Data;
using System.Data.Common;
using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Modules.Products.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Modules.Products.Controllers;

[Authorize(Policy = MaQuyen.SanPhamXem)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CategoryPagesController(ICategoryService categories, ILogger<CategoryPagesController> logger) : Controller
{
    [HttpGet("/Categories")]
    public async Task<IActionResult> Index([FromQuery] CategoryListQuery query, CancellationToken ct)
    {
        if (User.DangPhaiDoiMatKhau()) return Forbid();
        if (!ModelState.IsValid || !query.IsValid)
            return RenderError(400, "Trang và số dòng phải là số nguyên dương trong phạm vi cho phép.");
        try
        {
            return View("~/Modules/Products/Views/Categories/Index.cshtml",
                await categories.GetCategoriesAsync(query with { Size = query.NormalizedSize }, ct));
        }
        catch (Exception ex) when (ex is DbException or DataException)
        {
            logger.LogError(ex, "Không tải được danh sách danh mục.");
            return RenderError(503, "Chưa tải được danh mục. Vui lòng thử lại.");
        }
    }

    private IActionResult RenderError(int status, string message)
    {
        Response.StatusCode = status;
        ViewData["ListError"] = message;
        return View("~/Modules/Products/Views/Categories/Index.cshtml", new PagedResultDto<CategoryListItemDto>([], 0, 1, 20));
    }
}
