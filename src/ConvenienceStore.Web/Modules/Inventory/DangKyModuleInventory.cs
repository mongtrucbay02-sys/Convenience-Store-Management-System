using ConvenienceStore.Web.Modules.Inventory.Services;

namespace ConvenienceStore.Web.Modules.Inventory;

public static class DangKyModuleInventory
{
    public static IServiceCollection AddModuleInventory(this IServiceCollection services)
    {
        services.AddScoped<IDichVuKho, DichVuKho>();
        return services;
    }
}
