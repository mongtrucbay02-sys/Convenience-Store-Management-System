using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Auth;
using ConvenienceStore.Web.Modules.Auth.Services;
using ConvenienceStore.Web.Modules.Inventory;
using ConvenienceStore.Web.Modules.Products.Services;
using ConvenienceStore.Web.Modules.Reports;
using ConvenienceStore.Web.Modules.Reports.Services;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddAuthorization(options =>
    options.AddPolicy(
        AuthorizationPolicies.AdminOnly,
        policy => policy.RequireRole(AuthorizationPolicies.AdminRole)));

var connectionString =
    builder.Configuration.GetConnectionString("CuaHang")
    ?? throw new InvalidOperationException(
        "Thiếu chuỗi kết nối ConnectionStrings:CuaHang (biến môi trường ConnectionStrings__CuaHang).");

builder.Services.AddDbContext<CuaHangDbContext>(options =>
    options.UseMySQL(connectionString)
        .AddInterceptors(new GioVietNamInterceptor()));

builder.Services.Configure<AdminDauTienOptions>(
    builder.Configuration.GetSection(AdminDauTienOptions.TenMuc));
builder.Services.AddScoped<KhoiTaoAdminDauTien>();

builder.Services.AddXuLyLoiApi();
builder.Services.AddModuleAuth();
builder.Services.AddModuleInventory();

var app = builder.Build();

await KhoiTaoAdminDauTien.ChayKhiKhoiDongAsync(app.Services);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseXuLyLoiApi();
app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
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
