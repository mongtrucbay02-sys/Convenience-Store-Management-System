using ConvenienceStore.Web.Modules.Products.Services;

namespace ConvenienceStore.Web.Modules.Products;

public static class DangKyModuleProducts
{
    public static IServiceCollection AddModuleProducts(this IServiceCollection services)
    {
        services.AddScoped<ICategoryReadRepository, EfCategoryReadRepository>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ICategoryMutationRepository, EfCategoryMutationRepository>();
        services.AddScoped<CategoryMutationService>();
        services.AddScoped<IProductReadRepository, EfProductReadRepository>();
        services.AddScoped<ProductReadService>();
        services.AddScoped<IProductDetailRepository, EfProductDetailRepository>();
        services.AddScoped<ProductDetailService>();
        return services;
    }
}
