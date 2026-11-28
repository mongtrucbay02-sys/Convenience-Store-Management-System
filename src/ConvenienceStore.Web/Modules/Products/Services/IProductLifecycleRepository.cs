using ConvenienceStore.Web.Modules.Products.DTOs;

namespace ConvenienceStore.Web.Modules.Products.Services;

public interface IProductLifecycleRepository
{
    Task<ProductLifecyclePreview?> PreviewAsync(long id, CancellationToken ct = default);
    Task<ProductLifecyclePreview> ExecuteAsync(long id, ProductLifecycleOperation operation, ProductLifecycleCommand input, CancellationToken ct = default);
}
