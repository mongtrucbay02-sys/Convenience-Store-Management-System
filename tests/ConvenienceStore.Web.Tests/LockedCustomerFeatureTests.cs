using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using ConvenienceStore.Web.Modules.Customers.Controllers;
using ConvenienceStore.Web.Shared.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ConvenienceStore.Web.Tests;

public sealed class LockedCustomerFeatureTests : IAsyncLifetime
{
    private WebApplication app = null!;
    private HttpClient client = null!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Services.AddControllersWithViews().AddApplicationPart(typeof(CustomersController).Assembly);
        builder.Services.AddAuthentication("ScopeTest")
            .AddScheme<AuthenticationSchemeOptions, ScopeTestAuthentication>("ScopeTest", _ => { });
        builder.Services.AddAuthorization(options => options.AddPolicy("AdminOnly", policy => policy.RequireRole("AD")));
        // Deliberately no domain services, connection string or database: locked requests must stop before I/O.
        app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.MapControllerRoute("areas", "{area:exists}/{controller=Home}/{action=Index}/{id?}");
        app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
        await app.StartAsync();
        client = new HttpClient { BaseAddress = new Uri(app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses.Single()) };
    }

    [Theory]
    [InlineData("AD")]
    [InlineData("TN")]
    [InlineData("NK")]
    public async Task DirectRequestsCannotBypassScopeLock(string role)
    {
        client.DefaultRequestHeaders.Add("X-Scope-Test-Role", role);
        using var adjustment = await client.PostAsync("/Customers/Adjust/1",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["Points"] = "100", ["Reason"] = "test" }));
        Assert.Equal(HttpStatusCode.Forbidden, adjustment.StatusCode);
        if (role == "AD") Assert.Contains("KH-08", await adjustment.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task DirectServiceCallIsBlockedBeforeDatabaseAccess()
    {
        var service = new ConvenienceStore.Web.Modules.Customers.Services.CustomerService(null!);
        var error = await Assert.ThrowsAsync<ConvenienceStore.Web.Shared.Errors.LoiNghiepVu>(
            () => service.AdjustAsync(1, new() { Points = 100, Reason = "test" }, 1, default));
        Assert.Equal(403, error.MaHttp);
        Assert.Equal("CHUC_NANG_DANG_KHOA", error.Ma);
    }

    [Fact]
    public void HistoryAndCustomerReportsRemainInScope()
    {
        static void Unlocked(Type controller, string action) => Assert.Empty(controller.GetMethod(action)!
            .GetCustomAttributes(typeof(LockedFeatureAttribute), true));
        Unlocked(typeof(CustomersController), "History");
        Unlocked(typeof(ConvenienceStore.Web.Modules.Reports.Controllers.ReportsController), "Customers");
        Unlocked(typeof(ConvenienceStore.Web.Modules.Reports.Controllers.ReportsController), "Detail");
        Unlocked(typeof(ConvenienceStore.Web.Modules.Reports.Controllers.ReportsController), "Export");
        Unlocked(typeof(ConvenienceStore.Web.Modules.Reports.Controllers.OperationalReportsApiController), "Customers");
    }

    public async Task DisposeAsync()
    {
        client.Dispose();
        await app.DisposeAsync();
    }

    private sealed class ScopeTestAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var role = Request.Headers["X-Scope-Test-Role"].ToString();
            if (string.IsNullOrEmpty(role)) return Task.FromResult(AuthenticateResult.NoResult());
            var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], Scheme.Name));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
        }
    }
}
