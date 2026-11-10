using ConvenienceStore.Web.Modules.Products.DTOs;

namespace ConvenienceStore.Web.Modules.Products.Services;

public interface ICategoryMutationRepository
{
    Task<CategoryMutationResult> CreateAsync(CategoryMutationDto input,
        CancellationToken cancellationToken = default);
    Task<CategoryMutationResult> UpdateAsync(uint id, CategoryUpdateDto input,
        CancellationToken cancellationToken = default);
}
