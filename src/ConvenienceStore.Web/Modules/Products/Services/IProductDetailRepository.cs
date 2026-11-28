using ConvenienceStore.Web.Modules.Products.DTOs;

namespace ConvenienceStore.Web.Modules.Products.Services;

public interface IProductDetailRepository
{
    Task<ProductDetailsDto?> FindAsync(long id, bool activeOnly, CancellationToken ct = default);
    Task<PagedResultDto<ProductPriceDto>> PricesAsync(long id, ProductPricesQuery query, CancellationToken ct = default);
}
