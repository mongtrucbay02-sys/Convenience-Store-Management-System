using ConvenienceStore.Web.Modules.Products.DTOs;

namespace ConvenienceStore.Web.Modules.Products.Services;

public interface IProductReadRepository
{
    Task<PagedResultDto<ProductListItemDto>> ListAsync(ProductListQuery query, bool activeOnly, CancellationToken ct = default);
}
