
using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Auth;
using ConvenienceStore.Web.Modules.Auth.Services;
using ConvenienceStore.Web.Modules.Inventory;
using ConvenienceStore.Web.Modules.Products.Services;
using ConvenienceStore.Web.Modules.Reports;
using ConvenienceStore.Web.Modules.Reports.Services;
using ConvenienceStore.Web.Services.Pos;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. MVC VÀ WEB API
// ==========================================

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// ==========================================
// 2. KẾT NỐI MYSQL
// ==========================================

var connectionString =
    builder.Configuration.GetConnectionString("CuaHang");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Chưa cấu hình ConnectionStrings:CuaHang. " +
        "Hãy thiết lập bằng User Secrets hoặc " +
        "biến môi trường ConnectionStrings__CuaHang.");
}

builder.Services.AddDbContext<CuaHangDbContext>(options =>
{
    options.UseMySQL(connectionString)
        .AddInterceptors(new GioVietNamInterceptor());
});

// ==========================================
// 3. ĐĂNG KÝ SERVICE POS
// ==========================================

// POS-01: Danh mục và sản phẩm
builder.Services.AddScoped<MySqlPosCatalog>();

// POS-01, POS-02, POS-09: Giỏ hàng
builder.Services.AddScoped<PosCartService>();

// POS-05: Thanh toán tiền mặt
builder.Services.AddScoped<PosPaymentService>();

// POS-07: Hoàn tất đơn hàng
builder.Services.AddScoped<PosCheckoutService>();

// POS-07: Đọc hóa đơn và in K80
builder.Services.AddScoped<PosInvoiceService>();

// ==========================================
// 4. MYSQL STORE POS-07
// ==========================================

// Lưu hóa đơn và xử lý đơn hàng
builder.Services.AddScoped<
    IPosCheckoutStore,
    MySqlPosCheckoutStore>();

// Đọc hóa đơn từ MySQL
builder.Services.AddScoped<
    IPosInvoiceStore,
    MySqlPosInvoiceStore>();

// ==========================================
// 5. MODULE CỦA NHÓM
// ==========================================

// Xử lý lỗi API
builder.Services.AddXuLyLoiApi();

// Đăng nhập và phân quyền
builder.Services.AddModuleAuth();

// Quản lý tồn kho
builder.Services.AddModuleInventory();

// ==========================================
// 6. KHỞI TẠO ADMIN
// ==========================================

builder.Services.Configure<AdminDauTienOptions>(
    builder.Configuration.GetSection(
        AdminDauTienOptions.TenMuc));

builder.Services.AddScoped<KhoiTaoAdminDauTien>();

// ==========================================
// 7. BUILD APPLICATION
// ==========================================

var app = builder.Build();

await KhoiTaoAdminDauTien.ChayKhiKhoiDongAsync(
    app.Services);

// ==========================================
// 8. HTTP PIPELINE
// ==========================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseXuLyLoiApi();

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

// Xác thực phải đứng trước phân quyền
app.UseAuthentication();
app.UseAuthorization();

// ==========================================
// 9. ROUTING
// ==========================================

// Static assets
app.MapStaticAssets();

// Hỗ trợ MVC Areas nếu dự án có sử dụng
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Route MVC mặc định
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Hỗ trợ Attribute Routing
app.MapControllers();

// ==========================================
// 10. RUN
// ==========================================

app.Run();
