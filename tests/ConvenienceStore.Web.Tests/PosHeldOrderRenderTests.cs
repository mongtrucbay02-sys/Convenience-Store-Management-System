using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.DataProtection;
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

public class PosHeldOrderRenderTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RendersEmptyOrHeldOrders_WithEncodedNotesAndAntiforgery(bool hasOrder)
    {
        var builder=WebApplication.CreateBuilder(new WebApplicationOptions {
            ApplicationName=typeof(PosHeldOrderService).Assembly.GetName().Name,
            ContentRootPath=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../src/ConvenienceStore.Web")) });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        builder.Services.AddControllersWithViews().AddApplicationPart(typeof(PosHeldOrderService).Assembly);
        await using var app=builder.Build();
        app.MapControllerRoute("default","{controller=Home}/{action=Index}/{id?}");
        await app.StartAsync();
        using var scope=app.Services.CreateScope();var services=scope.ServiceProvider;
        var http=new DefaultHttpContext { RequestServices=services };http.Request.Scheme="https";http.Request.Host=new HostString("localhost");
        http.SetEndpoint(new Endpoint(null,EndpointMetadataCollection.Empty,"Render test"));
        var context=new ActionContext(http,new RouteData(),new ActionDescriptor());
        var engine=services.GetRequiredService<ICompositeViewEngine>();
        var view=engine.GetView(null,"/Views/Pos/_HeldOrders.cshtml",false);Assert.True(view.Success);
        var model=new PosPageViewModel {
            Cart=new PosCart { Items=hasOrder?[new PosCartItem { Name="Sữa",Quantity=1,UnitPrice=25000 }]:[] },
            HeldOrders=hasOrder?[new PosHeldOrder(7,new DateTime(2026,11,5,10,30,0),"Khách <script>alert(1)</script>",2,50000)]:[] };
        var data=new ViewDataDictionary<PosPageViewModel>(services.GetRequiredService<IModelMetadataProvider>(),new ModelStateDictionary()) { Model=model };
        var temp=new TempDataDictionary(http,services.GetRequiredService<ITempDataProvider>());
        using var writer=new StringWriter();
        await view.View.RenderAsync(new ViewContext(context,view.View,data,temp,writer,new HtmlHelperOptions()));
        string html=writer.ToString(),decoded=WebUtility.HtmlDecode(html);
        Assert.Contains("__RequestVerificationToken",html);Assert.Contains("/Pos/Hold",html);
        if(hasOrder) {
            Assert.Contains("/Pos/Recall",html);Assert.Contains("Đơn #7",decoded);
            Assert.DoesNotContain("<script>alert(1)</script>",html);
        } else {Assert.Contains("Chưa có đơn tạm giữ",decoded);Assert.Contains("disabled=",html);}
    }
}

