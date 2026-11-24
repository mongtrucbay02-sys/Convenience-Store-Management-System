using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ConvenienceStore.Web.Modules.Returns.Services;

public static class DangKyModuleReturns
{
    public static IServiceCollection AddModuleReturns(
        this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<IInvoiceLookupStore, MySqlInvoiceLookupStore>();
        services.AddScoped<InvoiceLookupService>();

        services.AddScoped<ReturnOrderService>();
        services.AddScoped<ExchangeService>();

        services.TryAddScoped<IReturnPointsGateway, ReturnPointsGateway>();

        return services;
    }
}