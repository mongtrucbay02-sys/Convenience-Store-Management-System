using ConvenienceStore.Web.Modules.Products.DTOs;

namespace ConvenienceStore.Web.Modules.Products.Services;

public interface IProductCreationRepository
{
    Task<long> CreateAsync(ProductCreateDto input, long actorId, string? imageUrl = null, CancellationToken ct = default);
}
