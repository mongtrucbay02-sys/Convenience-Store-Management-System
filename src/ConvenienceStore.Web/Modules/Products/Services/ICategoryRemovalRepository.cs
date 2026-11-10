using ConvenienceStore.Web.Modules.Products.DTOs;

namespace ConvenienceStore.Web.Modules.Products.Services;

public interface ICategoryRemovalRepository
{
    Task<CategoryRemovalResult> DeleteAsync(uint id, uint version,
        CancellationToken cancellationToken = default);
    Task<CategoryRemovalResult> HideAsync(uint id, uint version,
        CancellationToken cancellationToken = default);
}
