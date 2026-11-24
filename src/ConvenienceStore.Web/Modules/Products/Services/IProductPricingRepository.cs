using ConvenienceStore.Web.Modules.Products.DTOs;

namespace ConvenienceStore.Web.Modules.Products.Services;

public interface IProductPricingRepository
{
    Task<ProductPriceResult?> FindAsync(long id, long priceId, CancellationToken ct = default);
    Task<ProductPriceResult> CreateAsync(long id, ProductPriceChangeDto input, long actorId, CancellationToken ct = default);
    Task<ProductPriceResult> CancelAsync(long id, long priceId, long version, long actorId, CancellationToken ct = default);
}
