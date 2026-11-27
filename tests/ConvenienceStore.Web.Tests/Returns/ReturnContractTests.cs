using System.Reflection;
using System.Text.RegularExpressions;
using ConvenienceStore.Web.Modules.Returns.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace ConvenienceStore.Web.Tests.Returns;

public sealed class ReturnContractTests
{
    [Fact]
    public void ExchangeJsonRoutesMatchControllerAndDoNotAdvertiseMvcApprovalAsJson()
    {
        var text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Contracts", "pos-dt.openapi.yaml")).Replace("\r\n", "\n");
        foreach (var method in typeof(ReturnOrdersApiController).GetMethods().Where(m => m.Name.StartsWith("Exchange")))
        {
            var attribute = Assert.Single(method.GetCustomAttributes<HttpMethodAttribute>());
            var template = Regex.Replace(attribute.Template!, @"\{(\w+):long\}", "{$1}");
            Assert.Contains("\n  /returns/" + template + ":\n", text);
        }
        Assert.DoesNotContain("\n  /returns/{id}/exchange:", text);
        Assert.DoesNotContain("\n  /returns/{id}/approve:", text);
        Assert.Contains("x-mvc-dt06-routes:", text);
        Assert.Equal("Returns/Approvals", typeof(ReturnApprovalsController).GetCustomAttribute<RouteAttribute>()!.Template);
        foreach (var name in new[] { "Decide", "CompleteExchange", "Repreview", "Resubmit" })
            Assert.NotNull(typeof(ReturnApprovalsController).GetMethod(name)!.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
    }
}
