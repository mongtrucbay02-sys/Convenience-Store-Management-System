using ConvenienceStore.Web.Modules.Auth.Security;
using Microsoft.AspNetCore.Authorization;
using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Modules.Products.Services;
using Microsoft.AspNetCore.Mvc;
using ConvenienceStore.Web.Shared.Errors;

namespace ConvenienceStore.Web.Modules.Products.Controllers;

[Authorize(Policy = MaQuyen.SanPhamXem)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[ApiController]
[Route("api/v1/categories")]
public sealed class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<CategoryListItemDto>>> GetCategories(
        [FromQuery] CategoryListQuery query,
        CancellationToken cancellationToken)
    {
        if (!query.IsValid)
        {
            return BadRequest(new PhanHoiLoi(MaLoiChung.YeuCauKhongHopLe,
                "Trang và số dòng phải là số nguyên dương trong phạm vi cho phép.",
                null, HttpContext?.TraceIdentifier ?? string.Empty));
        }

        var effectiveQuery = query.Size > 100
            ? query with { Size = query.NormalizedSize }
            : query;

        var result = await _categoryService.GetCategoriesAsync(
            effectiveQuery, cancellationToken);

        return Ok(result);

    }

}
