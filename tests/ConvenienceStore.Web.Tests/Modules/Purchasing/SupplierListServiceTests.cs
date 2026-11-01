using ConvenienceStore.Web.Modules.Purchasing.DTOs;
using ConvenienceStore.Web.Modules.Purchasing.Services;
using Xunit;

namespace ConvenienceStore.Web.Tests.Modules.Purchasing;

public sealed class SupplierListServiceTests
{
    private sealed class Repository : ISupplierListRepository
    {
        public SupplierListQuery? Received;
        public int Calls;
        public Task<IReadOnlyList<SupplierListItemDto>> ListAsync(SupplierListQuery query,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            Received = query;
            return Task.FromResult<IReadOnlyList<SupplierListItemDto>>([]);
        }
    }

    [Fact]
    public async Task SearchAndStatus_AreNormalized()
    {
        var repo = new Repository();
        var result = await new SupplierListService(repo)
            .ListAsync(new("  Milk  ", " HOAT_DONG "));
        Assert.Empty(result);
        Assert.Equal("Milk", repo.Received?.Search);
        Assert.Equal("HOAT_DONG", repo.Received?.Status);
    }

    [Fact]
    public async Task InvalidStatus_DoesNotQuery()
    {
        var repo = new Repository();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            new SupplierListService(repo).ListAsync(new(Status: "UNKNOWN")));
        Assert.Equal(0, repo.Calls);
    }
}
