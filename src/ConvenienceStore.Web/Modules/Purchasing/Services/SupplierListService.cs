using ConvenienceStore.Web.Modules.Purchasing.DTOs;

namespace ConvenienceStore.Web.Modules.Purchasing.Services;

// HTTP exposure awaits the approved authentication mechanism for AD/NK.
public sealed class SupplierListService(ISupplierListRepository repository)
{
    public Task<IReadOnlyList<SupplierListItemDto>> ListAsync(SupplierListQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var status = query.Status?.Trim();
        if (!string.IsNullOrEmpty(status) &&
            status is not ("HOAT_DONG" or "NGUNG_HOP_TAC"))
            throw new ArgumentException("Unsupported supplier status.", nameof(query));
        return repository.ListAsync(query with
        {
            Search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim(),
            Status = string.IsNullOrEmpty(status) ? null : status
        }, cancellationToken);
    }
}
