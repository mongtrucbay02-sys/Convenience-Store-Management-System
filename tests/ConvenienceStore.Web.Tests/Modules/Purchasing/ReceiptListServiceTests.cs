using ConvenienceStore.Web.Modules.Purchasing.DTOs;
using ConvenienceStore.Web.Modules.Purchasing.Services;
using Xunit;
namespace ConvenienceStore.Web.Tests.Modules.Purchasing;
public sealed class ReceiptListServiceTests
{
    private sealed class Repository : IReceiptListRepository
    {
        public ReceiptListQuery? Received;
        public int Calls;
        public Task<IReadOnlyList<ReceiptListItemDto>> ListAsync(ReceiptListQuery query, CancellationToken cancellationToken = default)
        {
            Calls++;
            Received = query;
            return Task.FromResult<IReadOnlyList<ReceiptListItemDto>>([]);
        }
    }
    [Fact]
    public async Task Filters_AreNormalizedAndEmptyResultIsPreserved()
    {
        var repository = new Repository();
        var result = await new ReceiptListService(repository).ListAsync(new("  PN-001  ", 7, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31), " DA_NHAP_KHO "));
        Assert.Empty(result);
        Assert.Equal(1, repository.Calls);
        Assert.Equal("PN-001", repository.Received?.Code);
        Assert.Equal((uint)7, repository.Received?.SupplierId);
        Assert.Equal("DA_NHAP_KHO", repository.Received?.Status);
    }
    [Theory]
    [InlineData("INVALID")]
    [InlineData("")]
    public async Task UnsupportedStatus_DoesNotQuery(string status)
    {
        var repository = new Repository();
        if (status.Length == 0)
        {
            await new ReceiptListService(repository).ListAsync(new(Status: status));
            Assert.Null(repository.Received?.Status);
        }
        else
            await Assert.ThrowsAsync<ArgumentException>(() => new ReceiptListService(repository).ListAsync(new(Status: status)));
        Assert.Equal(status.Length == 0 ? 1 : 0, repository.Calls);
    }
    [Fact]
    public async Task ReversedDatesAndZeroSupplier_DoNotQuery()
    {
        var repository = new Repository();
        var service = new ReceiptListService(repository);
        await Assert.ThrowsAsync<ArgumentException>(() => service.ListAsync(new(From: new DateOnly(2026, 11, 2), To: new DateOnly(2026, 11, 1))));
        await Assert.ThrowsAsync<ArgumentException>(() => service.ListAsync(new(SupplierId: 0)));
        Assert.Equal(0, repository.Calls);
    }
}