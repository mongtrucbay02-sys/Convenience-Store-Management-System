using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Modules.Returns;
using ConvenienceStore.Web.Modules.Returns.Controllers;
using ConvenienceStore.Web.Modules.Returns.Services;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ConvenienceStore.Web.Tests.Returns;

public sealed class InvoiceLookupHttpTests
{
    private sealed class TestAuth(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var actor = Request.Headers["X-Test-Actor"].ToString();
            if (string.IsNullOrEmpty(actor)) return Task.FromResult(AuthenticateResult.NoResult());
            var user = InvoiceLookupServiceTests.Cashier(actor is "FORCED" or "NO_PERMISSION" ? "TN" : actor,
                permission: actor != "NO_PERMISSION", mustChange: actor == "FORCED");
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(user, Scheme.Name)));
        }
        protected override Task HandleChallengeAsync(AuthenticationProperties properties) =>
            PhanHoiLoiApi.GhiAsync(Context, 401, MaLoiChung.ChuaDangNhap, "Vui lòng đăng nhập.", null);
        protected override Task HandleForbiddenAsync(AuthenticationProperties properties) =>
            PhanHoiLoiApi.GhiAsync(Context, 403, MaLoiChung.KhongDuQuyen, "Không đủ quyền.", null);
    }

    private static async Task<WebApplication> Start(bool cancelled = false, bool empty = false)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions {
            ApplicationName = typeof(ReturnsController).Assembly.GetName().Name,
            ContentRootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/ConvenienceStore.Web"))
        });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        builder.Services.AddControllersWithViews().AddApplicationPart(typeof(ReturnsController).Assembly);
        builder.Services.AddXuLyLoiApi();
        builder.Services.AddModuleReturns();
        builder.Services.AddSingleton<TimeProvider>(new InvoiceLookupServiceTests.FixedTime(InvoiceLookupServiceTests.Now));
        builder.Services.AddScoped<IInvoiceLookupStore>(_ => new InvoiceLookupServiceTests.FakeStore(
            empty ? null : InvoiceLookupServiceTests.Invoice(cancelled ? "DA_HUY" : "DA_THANH_TOAN")));
        builder.Services.AddAuthentication("test").AddScheme<AuthenticationSchemeOptions, TestAuth>("test", _ => { });
        builder.Services.AddAuthorization(options => options.AddPolicy(MaQuyen.DoiTra, policy =>
            policy.RequireAuthenticatedUser().RequireClaim(ThongTinXacThuc.ClaimQuyen, MaQuyen.DoiTra)));
        var app = builder.Build();
        app.UseXuLyLoiApi(); app.UseRouting(); app.UseAuthentication(); app.UseAuthorization();
        app.MapControllers(); app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
        await app.StartAsync(); return app;
    }
    private static HttpClient Client(WebApplication app, string? actor)
    {
        var client = new HttpClient { BaseAddress = new Uri(app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses.Single()) };
        if (actor is not null) client.DefaultRequestHeaders.Add("X-Test-Actor", actor);
        return client;
    }

    [Theory]
    [InlineData(null, "/api/v1/returns/invoice-lookup?ma=HD-TEST", 401)]
    [InlineData(null, "/Returns", 401)]
    [InlineData("NK", "/api/v1/returns/invoice-lookup?ma=HD-TEST", 403)]
    [InlineData("AD", "/api/v1/returns/invoice-lookup?ma=HD-TEST", 403)]
    [InlineData("NO_PERMISSION", "/Returns?ma=HD-TEST", 403)]
    [InlineData("FORCED", "/Returns?ma=HD-TEST", 403)]
    [InlineData("FORCED", "/api/v1/returns/invoice-lookup?ma=HD-TEST", 403)]
    public async Task BothSurfacesEnforceAuthenticationAndCashierPermission(string? actor, string url, int expected)
    {
        await using var app = await Start(); using var client = Client(app, actor);
        var response = await client.GetAsync(url);
        Assert.Equal(expected, (int)response.StatusCode);
    }

    [Fact]
    public async Task ApiReturnsCommonPageAndDoesNotExposeFullPhone()
    {
        await using var app = await Start(); using var client = Client(app, "TN");
        var response = await client.GetAsync("/api/v1/returns/invoice-lookup?ma=HD-TEST");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl!.NoStore);
        var body = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);
        Assert.Equal(1, json.RootElement.GetProperty("total").GetInt32());
        Assert.Equal(9000, json.RootElement.GetProperty("items")[0].GetProperty("chiTiet")[0].GetProperty("giaThucTraMoiDonVi").GetInt64());
        Assert.DoesNotContain("0901234567", body);
    }

    [Theory]
    [InlineData("", 422)]
    [InlineData("?soDienThoai=abc", 422)]
    [InlineData("?ma=HD-TEST&page=abc", 400)]
    public async Task ApiUsesCommonValidationEnvelope(string query, int expected)
    {
        await using var app = await Start(); using var client = Client(app, "TN");
        var response = await client.GetAsync("/api/v1/returns/invoice-lookup" + query);
        Assert.Equal(expected, (int)response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.TryGetProperty("traceId", out _));
        Assert.True(json.RootElement.TryGetProperty("code", out _));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RazorPageRendersRealInvoiceAndCancellationState(bool cancelled)
    {
        await using var app = await Start(cancelled); using var client = Client(app, "TN");
        var response = await client.GetAsync("/Returns?ma=HD-TEST");
        var source = await response.Content.ReadAsStringAsync();
        var html = WebUtility.HtmlDecode(source);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("HD-TEST", html); Assert.Contains("Tên lúc bán", html);
        Assert.Contains("Đã trả / đang xử lý", html);
        Assert.Equal(cancelled, html.Contains("Hóa đơn đã hủy. Không được dùng"));
        Assert.DoesNotContain("0901234567", html);
        var previewDirectory = Environment.GetEnvironmentVariable("DT01_PREVIEW_DIR");
        if (!string.IsNullOrWhiteSpace(previewDirectory))
        {
            Directory.CreateDirectory(previewDirectory);
            await File.WriteAllTextAsync(Path.Combine(previewDirectory, cancelled ? "cancelled.html" : "paid.html"), source);
        }
    }

    [Fact]
    public async Task MissingInvoiceShowsE1AndReturnsEmptyApiPage()
    {
        await using var app = await Start(empty: true); using var client = Client(app, "TN");
        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/Returns?ma=NO-SUCH-INVOICE"));
        Assert.Contains("Không tìm thấy hóa đơn phù hợp", html);
        using var json = JsonDocument.Parse(await client.GetStringAsync("/api/v1/returns/invoice-lookup?ma=NO-SUCH-INVOICE"));
        Assert.Empty(json.RootElement.GetProperty("items").EnumerateArray());
        Assert.Equal(0, json.RootElement.GetProperty("total").GetInt32());
    }
}
