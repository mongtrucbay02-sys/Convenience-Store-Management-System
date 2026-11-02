using ConvenienceStore.Web.Modules.Purchasing.DTOs;

namespace ConvenienceStore.Web.Modules.Purchasing.Services;

public interface IReceiptListRepository
{
    Task<IReadOnlyList<ReceiptListItemDto>> ListAsync(ReceiptListQuery query,
        CancellationToken cancellationToken = default);
}