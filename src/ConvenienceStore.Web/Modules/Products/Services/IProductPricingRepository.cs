using ConvenienceStore.Web.Modules.Products.DTOs;

namespace ConvenienceStore.Web.Modules.Products.Services;

public interface IProductPricingRepository
{
    Task<ProductPriceResult?> FindAsync(uint id, uint priceId, CancellationToken ct = default);
    Task<ProductPriceResult> CreateAsync(uint id, ProductPriceChangeDto input, uint actorId, CancellationToken ct = default);
    Task<ProductPriceResult> CancelAsync(uint id, uint priceId, uint version, uint actorId, CancellationToken ct = default);
}
