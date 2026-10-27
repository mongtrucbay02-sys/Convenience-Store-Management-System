
using ConvenienceStore.Web.Services.Pos;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. Đăng ký MVC
// ==========================================
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

// POS-01, POS-02: Quản lý giỏ hàng
builder.Services.AddScoped<PosCartService>();

// POS-05: Thanh toán tiền mặt
// Gọi POS-07 tạm, chưa lưu hóa đơn và trừ kho
builder.Services.AddScoped<PosPaymentService>();

// ==========================================
// 4. Build ứng dụng
// ==========================================
var app = builder.Build();

// ==========================================
// 5. Cấu hình HTTP Pipeline
// ==========================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

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

// ==========================================
// 7. Chạy ứng dụng
// ==========================================
app.Run();
