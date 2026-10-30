using ConvenienceStore.Web.Modules.Products.DTOs;

namespace ConvenienceStore.Web.Modules.Products.Services;

// HTTP actions await an authenticated Admin/Manager contract.
public sealed class CategoryRemovalService(ICategoryRemovalRepository repository)
{
    public Task<CategoryRemovalResult> DeleteAsync(uint id, uint version,
        CancellationToken cancellationToken = default)
        => id == 0 || version == 0
            ? Task.FromResult(new CategoryRemovalResult(CategoryRemovalStatus.Invalid))
            : repository.DeleteAsync(id, version, cancellationToken);

    public Task<CategoryRemovalResult> HideAsync(uint id, uint version,
        CancellationToken cancellationToken = default)
        => id == 0 || version == 0
            ? Task.FromResult(new CategoryRemovalResult(CategoryRemovalStatus.Invalid))
            : repository.HideAsync(id, version, cancellationToken);
}
