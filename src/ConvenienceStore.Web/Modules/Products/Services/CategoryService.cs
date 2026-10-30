using ConvenienceStore.Web.Modules.Products.DTOs;
namespace ConvenienceStore.Web.Modules.Products.Services;
public sealed class CategoryService : ICategoryService
{
    private readonly ICategoryReadRepository _repository;
    public CategoryService(ICategoryReadRepository repository)
    {
        _repository = repository;
    }
    public Task<PagedResultDto<CategoryListItemDto>> GetCategoriesAsync(
        CategoryListQuery query,
        CancellationToken cancellationToken = default)
    {
        return _repository.GetCategoriesAsync(query, cancellationToken);
    }
}