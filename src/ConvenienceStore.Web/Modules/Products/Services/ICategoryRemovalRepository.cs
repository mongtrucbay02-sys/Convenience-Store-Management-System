using ConvenienceStore.Web.Modules.Products.DTOs;

namespace ConvenienceStore.Web.Modules.Products.Services;

public interface ICategoryRemovalRepository
{
    Task<CategoryRemovalPreviewDto?> PreviewAsync(long id, CancellationToken ct = default);
    Task<CategoryRemovalResult> DeleteAsync(long id, long version, CancellationToken ct = default);
    Task<CategoryRemovalResult> HideAsync(long id, long version, CancellationToken ct = default);
}
