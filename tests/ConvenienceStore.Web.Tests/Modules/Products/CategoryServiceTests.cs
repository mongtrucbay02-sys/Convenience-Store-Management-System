
using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Modules.Products.Services;
using Xunit;

namespace ConvenienceStore.Web.Tests.Modules.Products;

public class CategoryServiceTests
{
    private sealed class FakeCategoryReadRepository
        : ICategoryReadRepository
    {
        public CategoryListQuery? ReceivedQuery { get; private set; }

        public CancellationToken ReceivedToken { get; private set; }

        public int CallCount { get; private set; }

        public PagedResultDto<CategoryListItemDto> Response { get; set; }
            = new([], 0, 1, 20);

        public Task<PagedResultDto<CategoryListItemDto>> GetCategoriesAsync(
            CategoryListQuery query,
            CancellationToken cancellationToken = default)
        {
            ReceivedQuery = query;
            ReceivedToken = cancellationToken;
            CallCount++;

            return Task.FromResult(Response);
        }
    }

    [Fact]
    public async Task GetCategories_ForwardsQueryToRepository()
    {
        var repository = new FakeCategoryReadRepository();
        var service = new CategoryService(repository);

        var query = new CategoryListQuery
        {
            Page = 2,
            Size = 10
        };

        await service.GetCategoriesAsync(query);

        Assert.Same(query, repository.ReceivedQuery);
        Assert.Equal(1, repository.CallCount);
    }

    [Fact]
    public async Task GetCategories_ForwardsCancellationToken()
    {
        var repository = new FakeCategoryReadRepository();
        var service = new CategoryService(repository);

        using var cts = new CancellationTokenSource();

        await service.GetCategoriesAsync(
            new CategoryListQuery(),
            cts.Token);

        Assert.Equal(cts.Token, repository.ReceivedToken);
    }

    [Fact]
    public async Task GetCategories_ReturnsRepositoryResult()
    {
        var expected = new PagedResultDto<CategoryListItemDto>(
            [
                new CategoryListItemDto(
                    1u,
                    "Do uong",
                    false,
                    3,
                    "HOAT_DONG",
                    1u)
            ],
            1,
            1,
            20);

        var repository = new FakeCategoryReadRepository
        {
            Response = expected
        };

        var service = new CategoryService(repository);

        var result = await service.GetCategoriesAsync(
            new CategoryListQuery());

        Assert.Same(expected, result);
        Assert.Single(result.Items);
        Assert.Equal(3, result.Items[0].SoSanPham);
    }
}
