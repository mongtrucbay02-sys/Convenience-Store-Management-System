using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Auth;
using ConvenienceStore.Web.Modules.Auth.Services;
using ConvenienceStore.Web.Modules.Inventory;
using ConvenienceStore.Web.Modules.Products.Services;
using ConvenienceStore.Web.Modules.Reports;
using ConvenienceStore.Web.Modules.Reports.Services;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.EntityFrameworkCore;


using ConvenienceStore.Web.Services.Pos;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// ==========================================
// 2. Cấu hình Session cho giỏ hàng POS
// ==========================================
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();

// ==========================================
// 3. Đăng ký các dịch vụ POS
// ==========================================

// POS-01: Tìm kiếm và thêm sản phẩm
builder.Services.AddScoped<MySqlPosCatalog>();

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
// POS-01, POS-02: Quản lý giỏ hàng
builder.Services.AddScoped<PosCartService>();

builder.Services.AddXuLyLoiApi();
builder.Services.AddModuleAuth();
builder.Services.AddModuleInventory();
// POS-05: Thanh toán tiền mặt
// Gọi POS-07 tạm, chưa lưu hóa đơn và trừ kho
builder.Services.AddScoped<PosPaymentService>();

// ==========================================
// 4. Build ứng dụng
// ==========================================
var app = builder.Build();

await KhoiTaoAdminDauTien.ChayKhiKhoiDongAsync(app.Services);

// ==========================================
// 5. Cấu hình HTTP Pipeline
// ==========================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseXuLyLoiApi();
app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
// Nếu nhóm đã tích hợp Authentication,
// cần cấu hình AddAuthentication và
// app.UseAuthentication() trước UseAuthorization.
app.UseAuthorization();

// Bật Session cho giỏ hàng POS
app.UseSession();

// Phục vụ tài nguyên tĩnh
app.MapStaticAssets();

// ==========================================
// 6. Cấu hình Route mặc định
// ==========================================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
