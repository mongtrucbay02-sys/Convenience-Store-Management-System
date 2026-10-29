using System.Net;
using ConvenienceStore.Web.Models.Pos;
using ConvenienceStore.Web.Services.Pos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ConvenienceStore.Web.Tests;

public class PosInvoiceReceiptRenderTests
{
    private sealed class TempData : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task SharedK80RendersOriginalReprintAndCancelledMarkers(bool reprint, bool cancelled)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions {
            ApplicationName = typeof(PosInvoiceService).Assembly.GetName().Name,
            ContentRootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/ConvenienceStore.Web"))
        });
        builder.Services.AddControllersWithViews().AddApplicationPart(typeof(PosInvoiceService).Assembly);
        builder.Services.AddSingleton<ITempDataProvider, TempData>();
        await using var app = builder.Build();
        app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var context = new ActionContext(new DefaultHttpContext { RequestServices = services }, new RouteData(), new ActionDescriptor());
        var engine = services.GetRequiredService<ICompositeViewEngine>();
        var view = engine.GetView(null, "/Views/Shared/_ReceiptPaper.cshtml", false);
        Assert.True(view.Success, string.Join(", ", view.SearchedLocations ?? []));
        var model = new PosInvoiceViewModel {
            InvoiceId = 1, InvoiceCode = "HD-RENDER", Status = cancelled ? "DA_HUY" : "DA_THANH_TOAN",
            IsReprint = reprint, CreatedAt = new DateTime(2026, 10, 29), CustomerName = "Khách <script>",
            PointsEarned = 7, Subtotal = 100000, Total = 90000, Discount = 10000,
            Items = [new() { ProductName = "Tên lúc bán", Quantity = 2, UnitPrice = 50000, LineTotal = 90000, Discount = 10000 }]
        };
        using var writer = new StringWriter();
        var data = new ViewDataDictionary<PosInvoiceViewModel>(services.GetRequiredService<IModelMetadataProvider>(), new ModelStateDictionary()) { Model = model };
        var tempData = new TempDataDictionary(context.HttpContext, services.GetRequiredService<ITempDataProvider>());
        await view.View.RenderAsync(new ViewContext(context, view.View, data, tempData, writer, new HtmlHelperOptions()));
        var html = writer.ToString();
        var text = WebUtility.HtmlDecode(html);
        Assert.Contains("receipt-print-area", html);
        Assert.Contains("Tên lúc bán", text);
        Assert.Contains("Điểm đã cộng lúc bán:", text);
        Assert.Equal(reprint, text.Contains(">In lại</div>"));
        Assert.Equal(cancelled, text.Contains("ĐÃ HỦY"));
        Assert.DoesNotContain("Khách <script>", html); // Razor phải mã hóa nội dung khách hàng.
        if (!cancelled) Assert.Contains("Đã thanh toán", text);
    }
}
