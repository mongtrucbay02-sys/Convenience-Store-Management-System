using ConvenienceStore.Web.Modules.Purchasing.DTOs;

namespace ConvenienceStore.Web.Modules.Purchasing.Services;

// Authentication and Admin/Manager mapping must be established before HTTP exposure.
public sealed class SupplierDiscontinuationService(ISupplierDiscontinuationRepository repository)
{
    public Task<SupplierDiscontinuationResult> DiscontinueAsync(uint id, uint version,
        CancellationToken cancellationToken = default)
        => id == 0 || version == 0
            ? Task.FromResult(new SupplierDiscontinuationResult(SupplierDiscontinuationStatus.Invalid))
            : repository.DiscontinueAsync(id, version, cancellationToken);

    public Task<SupplierDiscontinuationResult> DeleteUnusedAsync(uint id, uint version,
        CancellationToken cancellationToken = default)
        => id == 0 || version == 0
            ? Task.FromResult(new SupplierDiscontinuationResult(SupplierDiscontinuationStatus.Invalid))
            : repository.DeleteUnusedAsync(id, version, cancellationToken);
}
