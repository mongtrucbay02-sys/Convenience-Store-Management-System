
using ConvenienceStore.Web.Services.Pos;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. ĐĂNG KÝ MVC
// ==========================================
builder.Services.AddControllersWithViews();

// ==========================================
// 2. CẤU HÌNH SESSION CHO GIỎ HÀNG POS
// ==========================================
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddHttpContextAccessor();

// ==========================================
// 3. ĐĂNG KÝ CÁC DỊCH VỤ POS
// ==========================================

// POS-01: Tìm kiếm và thêm sản phẩm
builder.Services.AddScoped<MySqlPosCatalog>();

// POS-01, POS-02: Quản lý giỏ hàng
builder.Services.AddScoped<PosCartService>();

// POS-05: Thanh toán tiền mặt
// POS-07 hiện mới là xử lý tạm thời
builder.Services.AddScoped<PosPaymentService>();

// POS-09: Hủy đơn đang bán
// BR-POS-10: Ghi lịch sử hủy vào MySQL
builder.Services.AddScoped<PosCancellationService>();

// ==========================================
// 4. CẤU HÌNH AUTHENTICATION
// ==========================================

// Sử dụng Cookie Authentication
builder.Services
    .AddAuthentication(
        CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        // Đường dẫn đăng nhập của nhóm
        options.LoginPath = "/Account/Login";

        // Đường dẫn khi không đủ quyền
        options.AccessDeniedPath = "/Account/AccessDenied";

        // Cấu hình Cookie
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy =
            CookieSecurePolicy.Always;

        options.Cookie.SameSite = SameSiteMode.Lax;

        // Thời gian tồn tại phiên đăng nhập
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);

        options.SlidingExpiration = true;
    });

// Đăng ký Authorization
builder.Services.AddAuthorization();

// ==========================================
// 5. BUILD ỨNG DỤNG
// ==========================================
var app = builder.Build();

// ==========================================
// 6. CẤU HÌNH HTTP PIPELINE
// ==========================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

// Xác thực tài khoản đăng nhập
app.UseAuthentication();

// Kiểm tra quyền truy cập
app.UseAuthorization();

// Bật Session cho giỏ hàng POS
app.UseSession();

// Phục vụ tài nguyên tĩnh
app.MapStaticAssets();

// ==========================================
// 7. CẤU HÌNH ROUTE MẶC ĐỊNH
// ==========================================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// ==========================================
// 8. CHẠY ỨNG DỤNG
// ==========================================
app.Run();
