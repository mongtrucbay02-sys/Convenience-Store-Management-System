using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Auth;
using ConvenienceStore.Web.Modules.Auth.Services;
using ConvenienceStore.Web.Modules.Inventory;
using ConvenienceStore.Web.Services.Pos;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. MVC VÀ HTTP CONTEXT
// ==========================================

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// ==========================================
// 2. SESSION CHO GIỎ HÀNG POS
// ==========================================

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);

    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;

    // HTTPS dùng cookie Secure; vẫn hỗ trợ HTTP khi phát triển.
    options.Cookie.SecurePolicy =
        CookieSecurePolicy.SameAsRequest;
});

// ==========================================
// 3. KẾT NỐI MYSQL
// ==========================================

// Dùng chung khóa với MySqlPosCatalog.
// Không ghi mật khẩu trực tiếp trong Program.cs.
var connectionString =
    builder.Configuration.GetConnectionString(
        "DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Chưa cấu hình ConnectionStrings:DefaultConnection. " +
        "Hãy cấu hình bằng User Secrets hoặc biến môi trường " +
        "ConnectionStrings__DefaultConnection.");
}

builder.Services.AddDbContext<CuaHangDbContext>(options =>
{
    options
        .UseMySQL(connectionString)
        .AddInterceptors(new GioVietNamInterceptor());
});

// ==========================================
// 4. ĐĂNG KÝ DỊCH VỤ POS
// ==========================================

// POS-01: Tìm kiếm sản phẩm.
builder.Services.AddScoped<MySqlPosCatalog>();

// POS-01, POS-02: Quản lý giỏ hàng.
builder.Services.AddScoped<PosCartService>();

// POS-05: Thanh toán tiền mặt.
builder.Services.AddScoped<PosPaymentService>();

// POS-09: Hủy đơn và ghi nhận lịch sử hủy.
builder.Services.AddScoped<PosCancellationService>();

// ==========================================
// 5. MODULE AUTH, INVENTORY VÀ XỬ LÝ LỖI
// ==========================================

builder.Services.AddXuLyLoiApi();

// Module AUTH tự đăng ký Authentication và các policy.
// Không đăng ký thêm AddCookie riêng tại đây.
builder.Services.AddModuleAuth();

builder.Services.AddModuleInventory();

// ==========================================
// 6. KHỞI TẠO ADMIN ĐẦU TIÊN
// ==========================================

builder.Services.Configure<AdminDauTienOptions>(
    builder.Configuration.GetSection(
        AdminDauTienOptions.TenMuc));

builder.Services.AddScoped<KhoiTaoAdminDauTien>();

// ==========================================
// 7. BUILD ỨNG DỤNG
// ==========================================

var app = builder.Build();

// Chỉ tạo admin nếu đáp ứng điều kiện của module AUTH.
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

// Cho phép các thành phần phía sau sử dụng session.
app.UseSession();

// Thứ tự: xác thực trước, phân quyền sau.
app.UseAuthentication();
app.UseAuthorization();

// ==========================================
// 9. ROUTING
// ==========================================

app.MapStaticAssets();

// Hỗ trợ các controller thuộc MVC Area.
app.MapControllerRoute(
    name: "areas",
    pattern:
        "{area:exists}/{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Route MVC mặc định.
app.MapControllerRoute(
    name: "default",
    pattern:
        "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Các API dùng attribute routing,
// ví dụ /api/v1/auth/login.
app.MapControllers();

// ==========================================
// 10. CHẠY ỨNG DỤNG
// ==========================================

app.Run();