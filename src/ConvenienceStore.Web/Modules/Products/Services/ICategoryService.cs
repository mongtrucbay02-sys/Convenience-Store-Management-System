
using ConvenienceStore.Web.Modules.Products.DTOs;

namespace ConvenienceStore.Web.Modules.Products.Services;

/// <summary>
/// Contract truy van danh sach danh muc SP-07.
/// Chua phu thuoc vao database provider.
/// </summary>
public interface ICategoryService
{
    Task<PagedResultDto<CategoryListItemDto>> GetCategoriesAsync(
        CategoryListQuery query,
        CancellationToken cancellationToken = default);
}
