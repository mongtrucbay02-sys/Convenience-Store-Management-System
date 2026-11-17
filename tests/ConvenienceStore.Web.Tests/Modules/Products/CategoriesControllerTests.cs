
using ConvenienceStore.Web.Modules.Products.Controllers;
using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Modules.Products.Services;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace ConvenienceStore.Web.Tests.Modules.Products;

public class CategoriesControllerTests
{
    private sealed class FakeCategoryService : ICategoryService
    {
        public PagedResultDto<CategoryListItemDto> Response { get; set; }
            = new([], 0, 1, 20);

        public int CallCount { get; private set; }
        public CategoryListQuery? ReceivedQuery { get; private set; }
        public CancellationToken ReceivedToken { get; private set; }

        public Task<PagedResultDto<CategoryListItemDto>> GetCategoriesAsync(
            CategoryListQuery query,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            ReceivedQuery = query;
            ReceivedToken = cancellationToken;
            return Task.FromResult(Response);
        }
    }

    [Fact]
    public async Task ValidQuery_ReturnsOkWithCategories()
    {
        var service = new FakeCategoryService
        {
            Response = new(
                [new CategoryListItemDto(
                    1u, "Do uong", false, 3, "HOAT_DONG", 1u)],
                1, 1, 20)
        };

        var controller = new CategoriesController(service);

        var result = await controller.GetCategories(
            new CategoryListQuery(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var data = Assert.IsType<PagedResultDto<CategoryListItemDto>>(ok.Value);

        Assert.Single(data.Items);
        Assert.Equal(3, data.Items[0].SoSanPham);
        Assert.Equal(1, service.CallCount);
    }

    [Fact]
    public async Task EmptyCategories_ReturnsEmptyPagedResult()
    {
        var service = new FakeCategoryService();
        var controller = new CategoriesController(service);

        var result = await controller.GetCategories(
            new CategoryListQuery(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var data = Assert.IsType<PagedResultDto<CategoryListItemDto>>(ok.Value);

        Assert.Empty(data.Items);
        Assert.Equal(0, data.Total);
    }

    [Fact]
    public async Task PageZero_ReturnsBadRequestWithoutCallingService()
    {
        var service = new FakeCategoryService();
        var controller = new CategoriesController(service);

        var result = await controller.GetCategories(
            new CategoryListQuery { Page = 0 }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(0, service.CallCount);
    }

    [Fact]
    public async Task SizeZero_ReturnsBadRequestWithoutCallingService()
    {
        var service = new FakeCategoryService();
        var controller = new CategoriesController(service);

        var result = await controller.GetCategories(
            new CategoryListQuery { Size = 0 }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(0, service.CallCount);
    }

    [Fact]
    public async Task ValidQuery_ForwardsQueryAndCancellationToken()
    {
        var service = new FakeCategoryService();
        var controller = new CategoriesController(service);
        var query = new CategoryListQuery { Page = 2, Size = 10 };
        using var cts = new CancellationTokenSource();

        var result = await controller.GetCategories(query, cts.Token);

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(query, service.ReceivedQuery);
        Assert.Equal(cts.Token, service.ReceivedToken);
        Assert.Equal(1, service.CallCount);
    }

    [Fact]
    public void SizeOver100_NormalizedSizeIs100()
    {
        var query = new CategoryListQuery { Page = 1, Size = 150 };
        Assert.Equal(100, query.NormalizedSize);
    }
    [Fact]
    public async Task SizeOver100_ForwardsNormalizedSizeToService()
    {
        var service = new FakeCategoryService();
        var controller = new CategoriesController(service);
        var query = new CategoryListQuery { Page = 1, Size = 150 };

        await controller.GetCategories(query, CancellationToken.None);

        Assert.NotNull(service.ReceivedQuery);
        Assert.Equal(100, service.ReceivedQuery.Size);
        Assert.Equal(1, service.CallCount);
    }
}