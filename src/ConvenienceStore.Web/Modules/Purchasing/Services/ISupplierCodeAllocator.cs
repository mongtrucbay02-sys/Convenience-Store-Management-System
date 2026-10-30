namespace ConvenienceStore.Web.Modules.Purchasing.Services;

// Business owner must approve code format/allocation before a production implementation is registered.
public interface ISupplierCodeAllocator
{
    Task<string> AllocateAsync(CancellationToken cancellationToken = default);
}
