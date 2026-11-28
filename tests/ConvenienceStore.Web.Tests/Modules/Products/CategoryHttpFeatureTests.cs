using System.Net;
using System.Text.Json;
using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Modules.Products.Services;
using ConvenienceStore.Web.Tests.HaTang;
using Microsoft.Extensions.DependencyInjection;
using ConvenienceStore.Web.Modules.Products;

namespace ConvenienceStore.Web.Tests.Modules.Products;

public sealed class CategoryHttpFeatureTests(CsdlPostgreSqlTam database) : IClassFixture<CsdlPostgreSqlTam>
{
    private static void Register(IServiceCollection services)
    {
        services.AddModuleProducts();
    }

    public static TheoryData<string, string?, int> Access
    {
        get
        {
            var data = new TheoryData<string, string?, int>();
            foreach (var path in new[] { "/Categories", "/api/v1/categories" })
            foreach (var actor in new string?[] { null, "AD", "NK", "TN", "FORCED", "LOCKED", "EXPIRED", "REVOKED", "FORGED" })
                data.Add(path, actor, actor is null or "LOCKED" or "EXPIRED" or "REVOKED" or "FORGED" ? 401 : actor == "FORCED" ? 403 : 200);
            return data;
        }
    }

    [Theory, MemberData(nameof(Access))]
    public async Task BothSurfacesUseRealSessions(string path, string? actor, int status)
    {
        await using var app = await ListFeatureHost.Start(database, Register);
        using var client = await ListFeatureHost.Client(app, database, actor);
        using var response = await client.GetAsync(path);
        Assert.Equal(status, (int)response.StatusCode);
        if (status == 200) Assert.True(response.Headers.CacheControl?.NoStore);
    }

    [Theory]
    [InlineData("AD", true)]
    [InlineData("NK", false)]
    [InlineData("TN", false)]
    public async Task EmptyPageOffersCreationGuidanceOnlyToAdmin(string role, bool guidance)
    {
        await using var app = await ListFeatureHost.Start(database, Register);
        using var client = await ListFeatureHost.Client(app, database, role);
        var html = await client.GetStringAsync("/Categories");
        Assert.Contains("data-empty=\"categories\"", html);
        Assert.Equal(guidance, html.Contains("data-create-guidance"));
        Assert.Contains("href=\"/Categories\"", html); // navigation
        Assert.Contains("name=\"size\"", html); // server-rendered paging form
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("page=-1")]
    [InlineData("size=0")]
    [InlineData("page=2147483647&size=100")]
    [InlineData("page=abc")]
    public async Task InvalidQueryHasCommonErrorContract(string query)
    {
        await using var app = await ListFeatureHost.Start(database, Register);
        using var client = await ListFeatureHost.Client(app, database, "AD");
        using var response = await client.GetAsync("/api/v1/categories?" + query);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("YEU_CAU_KHONG_HOP_LE", body.RootElement.GetProperty("code").GetString());
        Assert.False(string.IsNullOrEmpty(body.RootElement.GetProperty("traceId").GetString()));
    }

    private sealed class Failure : ICategoryService
    {
        public Task<PagedResultDto<CategoryListItemDto>> GetCategoriesAsync(CategoryListQuery query, CancellationToken ct = default) =>
            throw new System.Data.DataException("sensitive database detail");
    }

    [Fact]
    public async Task ReadFailureShowsRetryWithoutLeakingDetails()
    {
        await using var app = await ListFeatureHost.Start(database, s => { Register(s); s.AddScoped<ICategoryService, Failure>(); });
        using var client = await ListFeatureHost.Client(app, database, "AD");
        using var response = await client.GetAsync("/Categories");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("data-list-error", html);
        Assert.Contains("href=\"/Categories\"", html);
        Assert.DoesNotContain("sensitive database detail", html);
    }

    [Fact]
    public async Task PopulatedPageAndApiAgreeAndEncodeUntrustedCategoryNames()
    {
        const string name = "<script>alert(1)</script>";
        await database.ChayAsync("INSERT INTO danh_muc (id,ten) VALUES (920001,@name)", ("@name", name));
        try
        {
            await using var app = await ListFeatureHost.Start(database, Register);
            using var client = await ListFeatureHost.Client(app, database, "TN");
            var html = await client.GetStringAsync("/Categories?page=1&size=1");
            Assert.Contains("&lt;script&gt;", html);
            Assert.DoesNotContain("<script>alert(1)</script>", html);
            Assert.DoesNotContain("data-empty=", html);
            using var api = JsonDocument.Parse(await client.GetStringAsync("/api/v1/categories?page=1&size=1"));
            Assert.Equal(name, api.RootElement.GetProperty("items")[0].GetProperty("ten").GetString());
            Assert.Equal(0, api.RootElement.GetProperty("items")[0].GetProperty("soSanPham").GetInt32());
        }
        finally { await database.ChayAsync("DELETE FROM danh_muc WHERE id=920001"); }
    }
}
