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

// Cần cho các service đọc người dùng hiện tại (PosCartService lấy mã thu ngân từ claim).
builder.Services.AddHttpContextAccessor();

// ==========================================
// 2. KẾT NỐI MYSQL
// ==========================================

// Đọc từ User Secrets, appsettings.json hoặc biến môi trường ConnectionStrings__CuaHang.
// MySqlPosCatalog cũng phải dùng chính khóa "CuaHang" này (không dùng khóa riêng "PosMySql").
var connectionString =
    builder.Configuration.GetConnectionString("CuaHang");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Chưa cấu hình kết nối MySQL. " +
        "Hãy thiết lập ConnectionStrings:CuaHang " +
        "bằng .NET User Secrets hoặc biến môi trường " +
        "ConnectionStrings__CuaHang.");
}

builder.Services.AddDbContext<CuaHangDbContext>(options =>
{
    options.UseMySQL(connectionString)
        .AddInterceptors(new GioVietNamInterceptor());
});

// ==========================================
// 3. DỊCH VỤ POS
// ==========================================

// POS-01: tìm sản phẩm, lọc danh mục, giá bán hiệu lực, tồn kho.
builder.Services.AddScoped<MySqlPosCatalog>();

// POS-01, POS-02, POS-09: giỏ hàng lưu trong CSDL (gio_hang, ct_gio_hang),
// gắn với tài khoản thu ngân đang đăng nhập (BR-POS-03). Không dùng Session.
builder.Services.AddScoped<PosCartService>();

// POS-05: chuẩn bị thanh toán tiền mặt.
builder.Services.AddScoped<PosPaymentService>();

// ==========================================
// 4. MODULE CỦA NHÓM
// ==========================================

// Xử lý lỗi API theo định dạng của nhóm.
builder.Services.AddXuLyLoiApi();

// AUTH: đăng nhập, đăng xuất, xác thực phiên, phân quyền bằng Claim và Policy.
builder.Services.AddModuleAuth();

// INVENTORY: quản lý tồn kho.
builder.Services.AddModuleInventory();

// ==========================================
// 5. KHỞI TẠO TÀI KHOẢN ADMIN
// ==========================================

builder.Services.Configure<AdminDauTienOptions>(
    builder.Configuration.GetSection(
        AdminDauTienOptions.TenMuc));

builder.Services.AddScoped<KhoiTaoAdminDauTien>();

// ==========================================
// 6. BUILD ỨNG DỤNG
// ==========================================

var app = builder.Build();

// Giữ nguyên cơ chế khởi tạo Admin của module AUTH.
await KhoiTaoAdminDauTien.ChayKhiKhoiDongAsync(app.Services);

// ==========================================
// 7. HTTP PIPELINE (thứ tự quan trọng)
// ==========================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Middleware xử lý lỗi của nhóm (đặt sớm để bắt lỗi của các middleware phía sau).
app.UseXuLyLoiApi();

app.UseHttpsRedirection();

// File tĩnh trong wwwroot (css, js, images, login.html).
app.UseStaticFiles();

app.UseRouting();

// Xác thực trước, phân quyền sau.
// PosController phải có [Authorize(Policy = ...)] theo quyền BanHang của module AUTH.
app.UseAuthentication();
app.UseAuthorization();

// ==========================================
// 8. ROUTE
// ==========================================

app.MapStaticAssets();

// Route MVC mặc định.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// API Controller dùng attribute routing, ví dụ POST /api/v1/auth/login.
app.MapControllers();

app.Run();