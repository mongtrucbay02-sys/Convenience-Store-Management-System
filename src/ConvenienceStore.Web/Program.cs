using ConvenienceStore.Web.Modules.Products;
using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Auth;
using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Modules.Auth.Services;
using ConvenienceStore.Web.Modules.Inventory;
using ConvenienceStore.Web.Modules.Purchasing;
using ConvenienceStore.Web.Modules.Products.Services;
using ConvenienceStore.Web.Modules.Promotions.Services;
using ConvenienceStore.Web.Modules.Reports;
using ConvenienceStore.Web.Modules.Reports.Services;
using ConvenienceStore.Web.Modules.Returns;
using ConvenienceStore.Web.Modules.Returns.Services;
using ConvenienceStore.Web.Services.Pos;
using ConvenienceStore.Web.Shared.Data;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ======================================================
// 1. MVC VÀ WEB API
// ======================================================

builder.Services.AddControllersWithViews()
    .AddRazorOptions(options =>
    {
        options.ViewLocationFormats.Insert(
            0,
            "/Modules/PointOfSale/Views/{1}/{0}.cshtml");

        options.AreaViewLocationFormats.Insert(
            0,
            "/Modules/{2}/Views/{1}/{0}.cshtml");
    });

builder.Services.AddHttpContextAccessor();

// Customer Service
builder.Services.AddScoped<
    ConvenienceStore.Web.Modules.Customers.Services.CustomerService>();

// TimeProvider
builder.Services.AddSingleton(TimeProvider.System);

// Report Filter
builder.Services.AddScoped<ReportFilterService>();
builder.Services.AddScoped<PromotionUsageStore>();
builder.Services.AddScoped<IWeekEightReportDataSource, MySqlWeekEightReportDataSource>();
builder.Services.AddScoped<WeekEightReportsService>();

// ======================================================
// 2. DASHBOARD
// ======================================================

builder.Services.AddScoped<
    IDashboardDataSource,
    MySqlDashboardDataSource>();

builder.Services.AddScoped<
    IDashboardService,
    DashboardService>();

// ======================================================
// 3. BÁO CÁO
// ======================================================

builder.Services.AddScoped<
    IOperationalReportDataSource,
    MySqlTransactionalReportsDataSource>();

builder.Services.AddScoped<
    ITransactionalReportsDataSource,
    MySqlTransactionalReportsDataSource>();

builder.Services.AddScoped<
    IOperationalReportsService,
    OperationalReportsService>();

// ======================================================
// 4. KHUYẾN MÃI
// ======================================================

builder.Services.AddScoped<
    IPromotionDataSource,
    MySqlPromotionDataSource>();

builder.Services.AddScoped<
    IProductLookup,
    MySqlProductLookup>();

builder.Services.AddScoped<
    IPromotionService,
    PromotionService>();

// ======================================================
// 5. KẾT NỐI CƠ SỞ DỮ LIỆU
// ======================================================

var connectionString = MySqlEnv.GetConnectionString(
    builder.Environment.ContentRootPath,
    builder.Configuration);

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Chưa cấu hình ConnectionStrings:CuaHang. " +
        "Hãy đặt ConnectionStrings__CuaHang trong file .env " +
        "cạnh project ConvenienceStore.Web hoặc truyền qua biến môi trường.");
}

var databaseProvider = MySqlEnv.GetProvider(
    builder.Environment.ContentRootPath,
    builder.Configuration);

if (databaseProvider == "PostgreSql")
{
    connectionString = SqlDialect.PostgresConnectionString(connectionString);
}

builder.Configuration["ConnectionStrings:CuaHang"] = connectionString;

// Lưu provider để các module khác có thể sử dụng
builder.Configuration["Database:Provider"] = databaseProvider;

builder.Services.AddDbContext<CuaHangDbContext>(options =>
{
    if (databaseProvider == "PostgreSql")
    {
        options.UseNpgsql(
            connectionString,
            postgres => postgres.SetPostgresVersion(17, 0));
    }
    else
    {
        options.UseMySQL(connectionString)
            .AddInterceptors(new GioVietNamInterceptor());
    }
});

// ======================================================
// 6. POS-01, POS-02, POS-05, POS-07, POS-09
// ======================================================

// POS-01: Danh mục và sản phẩm
builder.Services.AddScoped<MySqlPosCatalog>();

// POS-01, POS-02, POS-09: Giỏ hàng
builder.Services.AddScoped<PosCartService>();
builder.Services.AddScoped<PosHeldOrderService>();

// POS-05: Thanh toán tiền mặt
builder.Services.AddScoped<PosPaymentService>();

// POS-07: Hoàn tất đơn hàng
builder.Services.AddScoped<PosCheckoutService>();

// POS-07: Đọc hóa đơn
builder.Services.AddScoped<PosInvoiceService>();

// POS-12: Hủy hóa đơn
builder.Services.AddScoped<PosInvoiceCancellationService>();

// Cash Ledger: Ghi nhận giao dịch tiền mặt
builder.Services.AddScoped<CashLedgerService>();

// POS-07: Store lưu đơn hàng
builder.Services.AddScoped<
    IPosCheckoutStore,
    MySqlPosCheckoutStore>();

// POS-07: Store đọc hóa đơn
builder.Services.AddScoped<
    IPosInvoiceStore,
    MySqlPosInvoiceStore>();

// ======================================================
// 7. POS-10, POS-11: TRA CỨU HÓA ĐƠN
// ======================================================

// POS-10: Tra cứu danh sách hóa đơn
builder.Services.AddScoped<PosInvoiceListService>();

builder.Services.AddScoped<
    IPosInvoiceListStore,
    MySqlPosInvoiceListStore>();

// ======================================================
// 8. CÁC MODULE CỦA HỆ THỐNG
// ======================================================

// Xử lý lỗi API
builder.Services.AddXuLyLoiApi();

// AUTH: Đăng nhập, đăng xuất, phân quyền
builder.Services.AddModuleAuth();

// INVENTORY: Quản lý kho
builder.Services.AddModuleInventory();

// PRODUCTS: Quản lý sản phẩm
builder.Services.AddModuleProducts();

// PURCHASING: Quản lý nhập hàng
builder.Services.AddScoped<PurchaseReceiptService>();

// RETURNS: Quản lý trả hàng và đổi hàng
builder.Services.AddModuleReturns();

// ======================================================
// 9. PHÂN QUYỀN
// ======================================================

builder.Services.AddAuthorization(options =>
{
    // DT-06: Duyệt đổi trả / cấu hình
    options.AddPolicy(
        MaQuyen.DoiTraDuyetCauHinh,
        policy =>
            policy.RequireRole(BangQuyen.Admin)
                .RequireClaim(
                    ThongTinXacThuc.ClaimQuyen,
                    MaQuyen.DoiTraDuyetCauHinh));

    // --------------------------------------------------
    // ADMIN
    // --------------------------------------------------

    options.AddPolicy(
        AuthorizationPolicies.AdminOnly,
        policy =>
        {
            policy.RequireAuthenticatedUser();

            policy.RequireRole(
                AuthorizationPolicies.AdminRole);
        });

    // --------------------------------------------------
    // POS: BÁN HÀNG
    // ADMIN HOẶC CÓ QUYỀN BAN_HANG
    // --------------------------------------------------

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

    // --------------------------------------------------
    // POS-10, POS-11: XEM HÓA ĐƠN
    // --------------------------------------------------

    options.AddPolicy(
        PosInvoiceAccess.PolicyXem,
        policy =>
        {
            policy.RequireAuthenticatedUser();

            policy.RequireAssertion(context =>
                PosInvoiceAccess.CoQuyenXem(
                    context.User));
        });

    // --------------------------------------------------
    // POS-12: HỦY HÓA ĐƠN
    // CHỈ ADMIN
    // --------------------------------------------------

    options.AddPolicy(
        MaQuyen.HoaDonHuy,
        policy =>
        {
            policy.RequireAuthenticatedUser();

            policy.RequireRole(
                AuthorizationPolicies.AdminRole);
        });
});

// ======================================================
// 10. KHỞI TẠO ADMIN ĐẦU TIÊN
// ======================================================

builder.Services.Configure<AdminDauTienOptions>(
    builder.Configuration.GetSection(
        AdminDauTienOptions.TenMuc));

builder.Services.AddScoped<KhoiTaoAdminDauTien>();

// ======================================================
// 11. BUILD APPLICATION
// ======================================================

var app = builder.Build();

// Khởi tạo Admin đầu tiên khi ứng dụng chạy
await KhoiTaoAdminDauTien.ChayKhiKhoiDongAsync(
    app.Services);

// ======================================================
// 12. HTTP PIPELINE
// ======================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");

    app.UseHsts();
}

// Xử lý lỗi API
app.UseXuLyLoiApi();

// HTTPS
app.UseHttpsRedirection();

// Static Files
app.UseStaticFiles();

// Routing
app.UseRouting();

// Authentication
app.UseAuthentication();

// Authorization
app.UseAuthorization();

// ======================================================
// 13. ROUTING
// ======================================================

app.MapStaticAssets();

// Route Areas
app.MapControllerRoute(
        name: "areas",
        pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Route mặc định
app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// API Controllers
app.MapControllers();

// ======================================================
// 14. RUN APPLICATION
// ======================================================

app.Run();