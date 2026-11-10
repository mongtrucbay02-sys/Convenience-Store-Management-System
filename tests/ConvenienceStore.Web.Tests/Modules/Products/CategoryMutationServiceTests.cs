using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Modules.Products.Services;
using Xunit;

namespace ConvenienceStore.Web.Tests.Modules.Products;

public sealed class CategoryMutationServiceTests
{
    private sealed class Repository : ICategoryMutationRepository
    {
        public int Calls;
        public CategoryMutationDto? Created;
        public CategoryUpdateDto? Updated;
        public Task<CategoryMutationResult> CreateAsync(CategoryMutationDto input,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            Created = input;
            return Task.FromResult(new CategoryMutationResult(CategoryMutationStatus.Success));
        }
        public Task<CategoryMutationResult> UpdateAsync(uint id, CategoryUpdateDto input,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            Updated = input;
            return Task.FromResult(new CategoryMutationResult(CategoryMutationStatus.Success));
        }
    }

    [Fact]
    public async Task Create_TrimsFieldsAndPreservesFlag()
    {
        var repo = new Repository();
        var result = await new CategoryMutationService(repo)
            .CreateAsync(new("  Fresh  ", "  Food  ", true));
        Assert.Equal(CategoryMutationStatus.Success, result.Status);
        Assert.Equal("Fresh", repo.Created?.Ten);
        Assert.Equal("Food", repo.Created?.MoTa);
        Assert.True(repo.Created?.KhongDoiTra);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_RejectsBlankNameWithoutWrite(string name)
    {
        var repo = new Repository();
        var result = await new CategoryMutationService(repo).CreateAsync(new(name, null, false));
        Assert.Equal(CategoryMutationStatus.Invalid, result.Status);
        Assert.Equal(0, repo.Calls);
    }

    [Fact]
    public async Task Create_RejectsMappedLengthOverflow()
    {
        var repo = new Repository();
        var result = await new CategoryMutationService(repo)
            .CreateAsync(new(new string('x', 101), null, false));
        Assert.Equal(CategoryMutationStatus.Invalid, result.Status);
        Assert.Equal(0, repo.Calls);
    }

    [Fact]
    public async Task Update_RequiresVersionAndForwardsFields()
    {
        var repo = new Repository();
        var service = new CategoryMutationService(repo);
        Assert.Equal(CategoryMutationStatus.Invalid,
            (await service.UpdateAsync(1, new("Fresh", null, false, 0))).Status);
        Assert.Equal(0, repo.Calls);
        Assert.Equal(CategoryMutationStatus.Success,
            (await service.UpdateAsync(1, new(" Fresh ", " Note ", true, 2))).Status);
        Assert.Equal("Fresh", repo.Updated?.Ten);
        Assert.Equal("Note", repo.Updated?.MoTa);
        Assert.Equal((uint)2, repo.Updated?.PhienBan);
    }
}
