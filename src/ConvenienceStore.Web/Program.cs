
using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Auth;
using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Modules.Auth.Services;
using ConvenienceStore.Web.Modules.Inventory;
using ConvenienceStore.Web.Modules.Products.Services;
using ConvenienceStore.Web.Modules.Promotions.Services;
using ConvenienceStore.Web.Modules.Reports;
using ConvenienceStore.Web.Modules.Reports.Services;
using ConvenienceStore.Web.Modules.Returns;
using ConvenienceStore.Web.Services.Pos;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. MVC VÀ WEB API
// ==========================================

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<
    ConvenienceStore.Web.Modules.Customers.CustomerService>();

builder.Services.AddSingleton(TimeProvider.System);

// Dashboard
builder.Services.AddScoped<
    IDashboardDataSource,
    MySqlDashboardDataSource>();

builder.Services.AddScoped<
    IDashboardService,
    DashboardService>();

// Báo cáo
builder.Services.AddScoped<
    IOperationalReportDataSource,
    MySqlTransactionalReportsDataSource>();

builder.Services.AddScoped<
    ITransactionalReportsDataSource,
    MySqlTransactionalReportsDataSource>();

builder.Services.AddScoped<
    IOperationalReportsService,
    OperationalReportsService>();

// Khuyến mãi
builder.Services.AddScoped<
    IPromotionDataSource,
    MySqlPromotionDataSource>();

builder.Services.AddScoped<
    IProductLookup,
    MySqlProductLookup>();

builder.Services.AddScoped<
    IPromotionService,
    PromotionService>();

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
// 3. SERVICE POS-01, POS-02, POS-05, POS-07, POS-09
// ==========================================

// POS-01: Danh mục và sản phẩm
builder.Services.AddScoped<MySqlPosCatalog>();

// POS-01, POS-02, POS-09: Giỏ hàng
builder.Services.AddScoped<PosCartService>();
builder.Services.AddScoped<PosHeldOrderService>();

// POS-05: Thanh toán tiền mặt
builder.Services.AddScoped<PosPaymentService>();

// POS-07: Hoàn tất đơn hàng
builder.Services.AddScoped<PosCheckoutService>();

// POS-07: Đọc và in hóa đơn
builder.Services.AddScoped<PosInvoiceService>();
builder.Services.AddScoped<PosInvoiceCancellationService>();

// POS-07: Store lưu đơn hàng
builder.Services.AddScoped<
    IPosCheckoutStore,
    MySqlPosCheckoutStore>();

// POS-07: Store đọc hóa đơn
builder.Services.AddScoped<
    IPosInvoiceStore,
    MySqlPosInvoiceStore>();

// ==========================================
// 4. SERVICE POS-10, POS-11
// ==========================================

// POS-10: Tra cứu danh sách hóa đơn
builder.Services.AddScoped<PosInvoiceListService>();

builder.Services.AddScoped<
    IPosInvoiceListStore,
    MySqlPosInvoiceListStore>();

// ==========================================
// 5. MODULE CỦA NHÓM
// ==========================================

// Xử lý lỗi API
builder.Services.AddXuLyLoiApi();

// AUTH: Đăng nhập và phân quyền
builder.Services.AddModuleAuth();

// Quản lý tồn kho
builder.Services.AddModuleInventory();

// Đổi trả
builder.Services.AddModuleReturns();

// ==========================================
// 6. PHÂN QUYỀN
// ==========================================

builder.Services.AddAuthorization(options =>
{
    // Quyền Admin theo cấu hình AUTH hiện tại
    options.AddPolicy(
        AuthorizationPolicies.AdminOnly,
        policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireRole(
                AuthorizationPolicies.AdminRole);
        });

    // POS: Admin hoặc tài khoản có quyền BAN_HANG
    options.AddPolicy(
        MaQuyen.BanHang,
        policy =>
        {
            policy.RequireAuthenticatedUser();

            policy.RequireAssertion(context =>
                context.User.IsInRole(
                    AuthorizationPolicies.AdminRole)
                ||
                context.User.HasClaim(
                    ThongTinXacThuc.ClaimQuyen,
                    MaQuyen.BanHang));
        });

    // POS-10, POS-11: Xem hóa đơn
    options.AddPolicy(
        PosInvoiceAccess.PolicyXem,
        policy =>
        {
            policy.RequireAuthenticatedUser();

            policy.RequireAssertion(context =>
                PosInvoiceAccess.CoQuyenXem(
                    context.User));
        });

    // POS-12: Chỉ Admin được hủy hóa đơn đã thanh toán
    options.AddPolicy(
        MaQuyen.HoaDonHuy,
        policy =>
        {
            policy.RequireAuthenticatedUser();

            policy.RequireRole(
                AuthorizationPolicies.AdminRole);
        });
});

// ==========================================
// 7. KHỞI TẠO ADMIN
// ==========================================

builder.Services.Configure<AdminDauTienOptions>(
    builder.Configuration.GetSection(
        AdminDauTienOptions.TenMuc));

builder.Services.AddScoped<KhoiTaoAdminDauTien>();

// ==========================================
// 8. BUILD APPLICATION
// ==========================================

var app = builder.Build();

await KhoiTaoAdminDauTien.ChayKhiKhoiDongAsync(
    app.Services);

// ==========================================
// 9. HTTP PIPELINE
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

app.UseAuthentication();
app.UseAuthorization();

// ==========================================
// 10. ROUTING
// ==========================================

app.MapStaticAssets();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllers();

// ==========================================
// 11. RUN
// ==========================================

app.Run();
