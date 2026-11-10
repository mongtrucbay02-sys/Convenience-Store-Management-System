using ConvenienceStore.Web.Modules.Purchasing.DTOs;
using ConvenienceStore.Web.Modules.Purchasing.Services;
using Xunit;

namespace ConvenienceStore.Web.Tests.Modules.Purchasing;

public sealed class SupplierDiscontinuationServiceTests
{
    private sealed class Repository : ISupplierDiscontinuationRepository
    {
        public int Calls;
        public Task<SupplierDiscontinuationResult> DiscontinueAsync(uint id, uint version,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(new SupplierDiscontinuationResult(
                SupplierDiscontinuationStatus.PendingReceipts));
        }
        public Task<SupplierDiscontinuationResult> DeleteUnusedAsync(uint id, uint version,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(new SupplierDiscontinuationResult(
                SupplierDiscontinuationStatus.HasReceiptHistory));
        }
    }

    [Fact]
    public async Task InvalidIdentityNeverTouchesRepository()
    {
        var repo = new Repository();
        var service = new SupplierDiscontinuationService(repo);
        Assert.Equal(SupplierDiscontinuationStatus.Invalid,
            (await service.DiscontinueAsync(0, 1)).Status);
        Assert.Equal(SupplierDiscontinuationStatus.Invalid,
            (await service.DeleteUnusedAsync(1, 0)).Status);
        Assert.Equal(0, repo.Calls);
    }

    [Fact]
    public async Task RepositoryGuardsArePropagated()
    {
        var repo = new Repository();
        var service = new SupplierDiscontinuationService(repo);
        Assert.Equal(SupplierDiscontinuationStatus.PendingReceipts,
            (await service.DiscontinueAsync(1, 1)).Status);
        Assert.Equal(SupplierDiscontinuationStatus.HasReceiptHistory,
            (await service.DeleteUnusedAsync(1, 1)).Status);
        Assert.Equal(2, repo.Calls);
    }
}
