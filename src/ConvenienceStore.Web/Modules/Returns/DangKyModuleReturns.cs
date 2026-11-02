using ConvenienceStore.Web.Modules.Returns.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ConvenienceStore.Web.Modules.Returns;

public static class DangKyModuleReturns
{
    public static IServiceCollection AddModuleReturns(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IInvoiceLookupStore, MySqlInvoiceLookupStore>();
        services.AddScoped<InvoiceLookupService>();
        return services;
    }
}
