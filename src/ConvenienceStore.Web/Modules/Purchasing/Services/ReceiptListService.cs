using ConvenienceStore.Web.Modules.Purchasing.DTOs;

namespace ConvenienceStore.Web.Modules.Purchasing.Services;

// HTTP access awaits integration with the approved AD/NK authentication policy.
public sealed class ReceiptListService(IReceiptListRepository repository)
{
    public Task<IReadOnlyList<ReceiptListItemDto>> ListAsync(ReceiptListQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.SupplierId == 0 || (query.From is not null && query.To is not null && query.From > query.To))
            throw new ArgumentException("Invalid supplier or date range.", nameof(query));
        var status = query.Status?.Trim();
        if (!string.IsNullOrEmpty(status) && status is not ("NHAP" or "DA_NHAP_KHO" or "DA_HUY"))
            throw new ArgumentException("Unsupported receipt status.", nameof(query));
        return repository.ListAsync(query with
        {
            Code = string.IsNullOrWhiteSpace(query.Code) ? null : query.Code.Trim(),
            Status = string.IsNullOrEmpty(status) ? null : status
        }, cancellationToken);
    }
}