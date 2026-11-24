using System.Net;
using ConvenienceStore.Web.Modules.Inventory;
using ConvenienceStore.Web.Modules.Returns;
using ConvenienceStore.Web.Modules.Returns.Services;
using ConvenienceStore.Web.Services.Pos;
using ConvenienceStore.Web.Tests.HaTang;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ConvenienceStore.Web.Tests;

public sealed class PosPageAuthorizationTests(CsdlMySqlTam database) : IClassFixture<CsdlMySqlTam>
{
    private Task<WebApplication> Start() => ListFeatureHost.Start(database, services =>
    {
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["ConnectionStrings:CuaHang"] = database.ChuoiKetNoi }).Build());
        services.AddHttpContextAccessor();
        services.AddDataProtection().UseEphemeralDataProtectionProvider();
        services.AddModuleInventory();
        services.AddModuleReturns();
        services.AddScoped<MySqlPosCatalog>();
        services.AddScoped<PosCartService>();
        services.AddScoped<PosHeldOrderService>();
        services.AddScoped<PosPaymentService>();
        services.AddScoped<PosCheckoutService>();
        services.AddScoped<CashLedgerService>();
        services.AddScoped<IPosCheckoutStore, MySqlPosCheckoutStore>();
        services.AddScoped<PosInvoiceService>();
        services.AddScoped<IPosInvoiceStore, MySqlPosInvoiceStore>();
    });

    [Theory]
    [InlineData(null, 401)]
    [InlineData("AD", 403)]
    [InlineData("NK", 403)]
    [InlineData("TN", 200)]
    public async Task SalesPageRequiresCashierWithoutThrowingForAdmin(string? actor, int expected)
    {
        await using var app = await Start();
        using var client = await ListFeatureHost.Client(app, database, actor);
        using var response = await client.GetAsync("/Pos");
        Assert.Equal(expected, (int)response.StatusCode);
    }

    [Fact]
    public async Task AdminCannotUseSalesActionsButRetainsReceiptAccess()
    {
        await using var app = await Start();
        using var client = await ListFeatureHost.Client(app, database, "AD");
        foreach (var action in new[] { "Checkout", "Cash" })
        {
            using var response = await client.GetAsync("/Pos/" + action);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
        foreach (var action in new[] { "Hold", "Recall", "Add", "Update", "Remove", "Calculate", "ConfirmCash", "CompleteOrder", "Cancel" })
        {
            using var response = await client.PostAsync("/Pos/" + action, new FormUrlEncodedContent([]));
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
        // A malformed invoice id reaches Receipt's validation rather than a cashier-only gate.
        using var receipt = await client.GetAsync("/Pos/Receipt?invoiceId=0");
        Assert.Equal(HttpStatusCode.BadRequest, receipt.StatusCode);
    }
}
