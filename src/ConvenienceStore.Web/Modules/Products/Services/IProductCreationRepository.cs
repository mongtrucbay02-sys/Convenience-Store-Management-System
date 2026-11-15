using ConvenienceStore.Web.Modules.Products.DTOs;

namespace ConvenienceStore.Web.Modules.Products.Services;

public interface IProductCreationRepository
{
    Task<uint> CreateAsync(ProductCreateDto input, uint actorId, string? imageUrl = null, CancellationToken ct = default);
}
