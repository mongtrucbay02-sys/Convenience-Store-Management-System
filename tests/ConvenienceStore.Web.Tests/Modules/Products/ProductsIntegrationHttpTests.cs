using System.Reflection;
using System.Text.Json;
using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Data.Entities;
using ConvenienceStore.Web.Modules.Inventory;
using ConvenienceStore.Web.Modules.Products;
using ConvenienceStore.Web.Tests.HaTang;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ConvenienceStore.Web.Tests.Modules.Products;

public sealed class ProductsIntegrationHttpTests(CsdlPostgreSqlTam database) : IClassFixture<CsdlPostgreSqlTam>
{
    [Theory]
    [InlineData(null, 401)]
    [InlineData("FORGED", 401)]
    [InlineData("FORCED", 403)]
    [InlineData("LOCKED", 401)]
    [InlineData("EXPIRED", 401)]
    [InlineData("REVOKED", 401)]
    [InlineData("AD", 302)]
    [InlineData("NK", 302)]
    [InlineData("TN", 302)]
    public async Task LegacyAreaLandingEnforcesRealSessionAndRedirectsToManagement(string? actor, int status)
    {
        await using var app = await ListFeatureHost.Start(database, s => { s.AddModuleProducts(); s.AddModuleInventory(); });
        using var client = await ListFeatureHost.Client(app, database, actor);
        using var response = await client.GetAsync("/Products/Products/Index"); Assert.Equal(status, (int)response.StatusCode);
        if(status == 302) Assert.Equal("/Products", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task RuntimeControllersRoutesDocumentationAndSingleContextAreConsistent()
    {
        await using var app = await ListFeatureHost.Start(database, s => { s.AddModuleProducts(); s.AddModuleInventory(); });
        using var client = await ListFeatureHost.Client(app, database, "AD"); Assert.Equal(200, (int)(await client.GetAsync("/api/v1/products")).StatusCode);
        var root = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "../.."));
        var documentation = await File.ReadAllTextAsync(Path.Combine(root, "docs/04-system-design/api/products.md"));
        var endpoints = ((IEndpointRouteBuilder)app).DataSources.SelectMany(x => x.Endpoints).OfType<RouteEndpoint>()
            .Select(x => new { Endpoint = x, Action = x.Metadata.GetMetadata<ControllerActionDescriptor>() })
            .Where(x => x.Action?.ControllerTypeInfo.Namespace?.EndsWith(".Modules.Products.Controllers") == true).ToArray();
        Assert.NotEmpty(endpoints); var errors = new List<string>();
        using var scope = app.Services.CreateScope(); var inventory = new List<object>(); var keys = new List<string>();
        foreach(var controller in endpoints.Select(x => x.Action!.ControllerTypeInfo.AsType()).Distinct()) {
            if(!controller.GetCustomAttributes<AuthorizeAttribute>(true).Any(x => !string.IsNullOrEmpty(x.Policy))) errors.Add(controller.Name + ": missing server permission policy");
            try { var instance = ActivatorUtilities.CreateInstance(scope.ServiceProvider, controller); if(instance is IDisposable disposable) disposable.Dispose(); }
            catch(Exception ex) { errors.Add(controller.Name + ": DI " + ex.Message); }
            if(controller.GetCustomAttribute<ApiControllerAttribute>() is not null && !controller.GetCustomAttributes<TagsAttribute>(true).SelectMany(x => x.Tags).Contains("SP")) errors.Add(controller.Name + ": missing SP API tag");
        }
        foreach(var item in endpoints.Where(x => x.Action!.AttributeRouteInfo is not null)) {
            var action = item.Action!; var route = "/" + action.AttributeRouteInfo!.Template!.TrimStart('/'); var verbs = item.Endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? [];
            var api = action.ControllerTypeInfo.GetCustomAttribute<ApiControllerAttribute>() is not null;
            foreach(var verb in verbs) keys.Add(verb + " " + route.ToLowerInvariant());
            if(api && !documentation.Contains(route, StringComparison.Ordinal)) errors.Add("Undocumented API: " + route);
            inventory.Add(new { route, verbs, controller = action.ControllerName, action = action.ActionName, api });
        }
        errors.AddRange(keys.GroupBy(x => x).Where(x => x.Count() > 1).Select(x => "Duplicate attribute route: " + x.Key));
        var context = scope.ServiceProvider.GetRequiredService<CuaHangDbContext>(); Assert.Same(context, scope.ServiceProvider.GetRequiredService<CuaHangDbContext>());
        Assert.Single(typeof(CuaHangDbContext).Assembly.GetTypes(), x => !x.IsAbstract && typeof(DbContext).IsAssignableFrom(x));
        Assert.True(context.Model.FindEntityType(typeof(DanhMuc))!.FindProperty(nameof(DanhMuc.PhienBan))!.IsConcurrencyToken);
        using(var another = app.Services.CreateScope()) Assert.NotSame(context, another.ServiceProvider.GetRequiredService<CuaHangDbContext>());
        var registrations = new ServiceCollection(); registrations.AddModuleProducts(); Assert.DoesNotContain(registrations.GroupBy(x => x.ServiceType), x => x.Count() > 1);
        var output = Path.Combine(root, "artifacts/sp-module/final"); Directory.CreateDirectory(output);
        await File.WriteAllTextAsync(Path.Combine(output, "route-inventory.json"), JsonSerializer.Serialize(inventory, new JsonSerializerOptions { WriteIndented = true }));
        Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors));
    }
}
