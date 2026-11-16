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
        services.AddScoped<IProductCreationRepository, EfProductCreationRepository>();
        services.AddScoped<ProductCreationService>();
        services.AddScoped<IProductImageRepository, EfProductImageRepository>();
        services.AddScoped<IProductImageStore>(sp => {
            var env = sp.GetRequiredService<IWebHostEnvironment>();
            return new DiskProductImageStore(Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads", "products"));
        });
        services.AddScoped<ProductImageService>();
        services.AddScoped<IProductEditRepository, EfProductEditRepository>();
        services.AddScoped<ProductEditService>();
        services.AddScoped<IProductPricingRepository, EfProductPricingRepository>();
        services.AddScoped<ProductPricingService>();
        return services;
    }
}
