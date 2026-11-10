using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Modules.Products.Services;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Modules.Products.Controllers;

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
        if (query.Page <= 0 || query.Size <= 0)
        {
            return BadRequest(new
            {
                Message = "Page and Size must be greater than zero."
            });
        }

        var effectiveQuery = query.Size > 100
            ? query with { Size = query.NormalizedSize }
            : query;

        var result = await _categoryService.GetCategoriesAsync(
            effectiveQuery, cancellationToken);

        return Ok(result);

    }

}
