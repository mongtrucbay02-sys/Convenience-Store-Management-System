using ConvenienceStore.Web.Modules.Products.Services;

namespace ConvenienceStore.Web.Modules.Products;

public static class DangKyModuleProducts
{
    public static IServiceCollection AddModuleProducts(this IServiceCollection services)
    {
        services.AddScoped<ICategoryReadRepository, EfCategoryReadRepository>();
        services.AddScoped<ICategoryService, CategoryService>();
        return services;
    }
}
