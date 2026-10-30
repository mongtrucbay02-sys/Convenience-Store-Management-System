using ConvenienceStore.Web.Modules.Purchasing.DTOs;

namespace ConvenienceStore.Web.Modules.Purchasing.Services;

public interface ISupplierMutationRepository
{
    Task<SupplierMutationResult> CreateAsync(string code, SupplierMutationDto input,
        CancellationToken cancellationToken = default);
    Task<SupplierMutationResult> UpdateAsync(uint id, SupplierUpdateDto input,
        CancellationToken cancellationToken = default);
}
