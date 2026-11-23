using ConvenienceStore.Web.Modules.Products.DTOs;

namespace ConvenienceStore.Web.Modules.Products.Services;

public interface ICategoryMutationRepository
{
    Task<CategoryDetailsDto?> FindAsync(long id, CancellationToken ct = default);
    Task<CategoryMutationResult> CreateAsync(CategoryMutationDto input,
        CancellationToken cancellationToken = default);
    Task<CategoryMutationResult> UpdateAsync(long id, CategoryUpdateDto input,
        CancellationToken cancellationToken = default);
}
