using ConvenienceStore.Web.Modules.Purchasing.DTOs;

namespace ConvenienceStore.Web.Modules.Purchasing.Services;

public interface ISupplierListRepository
{
    Task<IReadOnlyList<SupplierListItemDto>> ListAsync(SupplierListQuery query,
        CancellationToken cancellationToken = default);
}
