using ConvenienceStore.Web.Modules.Products.DTOs;

namespace ConvenienceStore.Web.Modules.Products.Services;

public interface IProductLifecycleRepository
{
    Task<ProductLifecyclePreview?> PreviewAsync(uint id, CancellationToken ct = default);
    Task<ProductLifecyclePreview> ExecuteAsync(uint id, ProductLifecycleOperation operation, ProductLifecycleCommand input, CancellationToken ct = default);
}
