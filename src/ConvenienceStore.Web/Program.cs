using ConvenienceStore.Web.Modules.Products.Services;
using ConvenienceStore.Web.Modules.Promotions.Services;
using ConvenienceStore.Web.Modules.Reports;
using ConvenienceStore.Web.Modules.Reports.Services;
using ConvenienceStore.Web.Data;
using Microsoft.EntityFrameworkCore;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.Configure<Microsoft.AspNetCore.Mvc.Razor.RazorViewEngineOptions>(
    options =>
    {
        options.AreaViewLocationFormats.Insert(
            0,
            "/Modules/{2}/Views/{1}/{0}.cshtml");

        options.AreaViewLocationFormats.Insert(
            1,
            "/Modules/{2}/Views/Shared/{0}.cshtml");
    });

builder.Services.AddScoped<IProductService, ProductService>();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IDashboardDataSource, SampleDashboardDataSource>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IPromotionDataSource, SamplePromotionDataSource>();
builder.Services.AddScoped<IPromotionService, PromotionService>();
builder.Services.AddAuthorization(options =>
    options.AddPolicy(
        AuthorizationPolicies.AdminOnly,
        policy => policy.RequireRole(AuthorizationPolicies.AdminRole)));

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Missing DefaultConnection configuration.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();



app.Run();
