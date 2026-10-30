using ConvenienceStore.Web.Modules.Purchasing.DTOs;

namespace ConvenienceStore.Web.Modules.Purchasing.Services;

public interface ISupplierDiscontinuationRepository
{
    Task<SupplierDiscontinuationResult> DiscontinueAsync(uint id, uint version,
        CancellationToken cancellationToken = default);
    Task<SupplierDiscontinuationResult> DeleteUnusedAsync(uint id, uint version,
        CancellationToken cancellationToken = default);
}
