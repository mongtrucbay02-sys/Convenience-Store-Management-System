using ConvenienceStore.Web.Modules.Products.DTOs;

namespace ConvenienceStore.Web.Modules.Products.Services;

public interface ICategoryReadRepository
{
    Task<PagedResultDto<CategoryListItemDto>> GetCategoriesAsync(
        CategoryListQuery query,
        CancellationToken cancellationToken = default);
}
