using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Modules.Products.Services;
using Xunit;

namespace ConvenienceStore.Web.Tests.Modules.Products;

public sealed class CategoryRemovalServiceTests
{
    private sealed class Repository : ICategoryRemovalRepository
    {
        public uint LastId;
        public uint LastVersion;
        public int Calls;
        public Task<CategoryRemovalResult> DeleteAsync(uint id, uint version,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            LastId = id;
            LastVersion = version;
            return Task.FromResult(new CategoryRemovalResult(CategoryRemovalStatus.Referenced));
        }
        public Task<CategoryRemovalResult> HideAsync(uint id, uint version,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            LastId = id;
            LastVersion = version;
            return Task.FromResult(new CategoryRemovalResult(CategoryRemovalStatus.Success));
        }
    }

    [Fact]
    public async Task Delete_RequiresIdAndVersion()
    {
        var repo = new Repository();
        var service = new CategoryRemovalService(repo);
        Assert.Equal(CategoryRemovalStatus.Invalid, (await service.DeleteAsync(0, 1)).Status);
        Assert.Equal(CategoryRemovalStatus.Invalid, (await service.DeleteAsync(1, 0)).Status);
        Assert.Equal(0, repo.Calls);
    }

    [Fact]
    public async Task Delete_PropagatesReferenceBlock()
    {
        var repo = new Repository();
        var result = await new CategoryRemovalService(repo).DeleteAsync(9, 2);
        Assert.Equal(CategoryRemovalStatus.Referenced, result.Status);
        Assert.Equal((uint)9, repo.LastId);
        Assert.Equal((uint)2, repo.LastVersion);
    }

    [Fact]
    public async Task Hide_RequiresVersionAndDelegates()
    {
        var repo = new Repository();
        var service = new CategoryRemovalService(repo);
        Assert.Equal(CategoryRemovalStatus.Invalid, (await service.HideAsync(1, 0)).Status);
        Assert.Equal(0, repo.Calls);
        Assert.Equal(CategoryRemovalStatus.Success, (await service.HideAsync(1, 1)).Status);
        Assert.Equal(1, repo.Calls);
    }
}
