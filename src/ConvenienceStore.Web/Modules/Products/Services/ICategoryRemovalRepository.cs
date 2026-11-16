using ConvenienceStore.Web.Modules.Products.DTOs;

namespace ConvenienceStore.Web.Modules.Products.Services;

public interface ICategoryRemovalRepository
{
    Task<CategoryRemovalPreviewDto?> PreviewAsync(uint id, CancellationToken ct = default);
    Task<CategoryRemovalResult> DeleteAsync(uint id, uint version, CancellationToken ct = default);
    Task<CategoryRemovalResult> HideAsync(uint id, uint version, CancellationToken ct = default);
}
