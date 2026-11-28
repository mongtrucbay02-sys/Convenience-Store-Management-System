using ConvenienceStore.Web.Modules.Products.DTOs;

namespace ConvenienceStore.Web.Modules.Products.Services;

public interface IProductEditRepository
{
    Task UpdateAsync(long id, ProductEditDto input, string? imageUrl = null, CancellationToken ct = default);
}
