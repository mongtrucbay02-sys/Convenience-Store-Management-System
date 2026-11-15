using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ConvenienceStore.Web.Modules.Products;
using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Tests.HaTang;

namespace ConvenienceStore.Web.Tests.Modules.Products;

public sealed class Sp08CategoryHttpTests(CsdlMySqlTam db) : IClassFixture<CsdlMySqlTam>
{
    [Theory]
    [InlineData(null, 401)]
    [InlineData("TN", 403)]
    [InlineData("NK", 403)]
    [InlineData("FORCED", 403)]
    [InlineData("LOCKED", 401)]
    [InlineData("EXPIRED", 401)]
    [InlineData("REVOKED", 401)]
    public async Task CreateEnforcesSessionAndAdmin(string? actor, int status)
    {
        await using var app = await ListFeatureHost.Start(db, s => s.AddModuleProducts());
        using var client = await ListFeatureHost.Client(app, db, actor);
        AddXsrf(client);
        Assert.Equal(status, (int)(await client.PostAsJsonAsync("/api/v1/categories", new { ten = "Never create", khongDoiTra = false })).StatusCode);
    }
    [Fact]
    public async Task CreateUpdateDuplicateAndStaleVersionUseRealDatabase()
    {
        await using var app = await ListFeatureHost.Start(db, s => s.AddModuleProducts());
        using var client = await ListFeatureHost.Client(app, db, "AD"); AddXsrf(client);
        var name = "SP08-" + Guid.NewGuid().ToString("N");
        var created = await client.PostAsJsonAsync("/api/v1/categories", new { ten = " " + name + " ", moTa = "Description", khongDoiTra = true, trangThai = "DA_AN" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var json = JsonDocument.Parse(await created.Content.ReadAsStringAsync()); var id = json.RootElement.GetProperty("id").GetUInt32(); var version = json.RootElement.GetProperty("phienBan").GetUInt32();
        Assert.Equal("HOAT_DONG", json.RootElement.GetProperty("trangThai").GetString()); Assert.Equal(name, json.RootElement.GetProperty("ten").GetString());
        Assert.NotNull(created.Headers.Location);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/v1/categories", new { ten = name.ToUpperInvariant() })).StatusCode);
        var input = new { ten = name + " edit", moTa = "Changed", khongDoiTra = false, phienBan = version };
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync("/api/v1/categories/" + id, input)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync("/api/v1/categories/" + id, input)).StatusCode);
        using var saved = JsonDocument.Parse(await client.GetStringAsync("/api/v1/categories/" + id));
        Assert.Equal(version + 1, saved.RootElement.GetProperty("phienBan").GetUInt32()); Assert.False(saved.RootElement.GetProperty("khongDoiTra").GetBoolean());
    }
    [Fact]
    public async Task MissingXsrfAndInvalidInputAreRejected()
    {
        await using var app = await ListFeatureHost.Start(db, s => s.AddModuleProducts());
        using var client = await ListFeatureHost.Client(app, db, "AD");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/v1/categories", new { ten = "No CSRF" })).StatusCode);
        AddXsrf(client);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await client.PostAsJsonAsync("/api/v1/categories", new { ten = "   " })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync("/api/v1/categories/4294967295", new { ten = "Missing", phienBan = 1 })).StatusCode);
    }
    [Theory]
    [InlineData("AD", 200)]
    [InlineData("NK", 403)]
    [InlineData("TN", 403)]
    public async Task RazorCreateFormUsesRealAuthorizationAndAntiforgery(string actor, int status)
    {
        await using var app = await ListFeatureHost.Start(db, s => s.AddModuleProducts());
        using var client = await ListFeatureHost.Client(app, db, actor);
        using var response = await client.GetAsync("/Categories/Create"); Assert.Equal(status, (int)response.StatusCode);
        if (status == 200)
        {
            var html = await response.Content.ReadAsStringAsync(); Assert.Contains("__RequestVerificationToken", html); Assert.Contains("name=\"Ten\"", html);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/Categories/Create", new FormUrlEncodedContent(new Dictionary<string, string> { { "Ten", "Missing token" } }))).StatusCode);
        }
    }

    [Fact]
    public async Task RazorPostValidatesBindingAndPersistsWithAntiforgery()
    {
        await using var app = await ListFeatureHost.Start(db, s => s.AddModuleProducts());
        using var client = await ListFeatureHost.Client(app, db, "AD");
        using var form = await client.GetAsync("/Categories/Create"); var html = await form.Content.ReadAsStringAsync();
        var token = System.Text.RegularExpressions.Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        Assert.NotEmpty(token); var cookie = form.Headers.GetValues("Set-Cookie").First().Split(';')[0];
        var session = client.DefaultRequestHeaders.GetValues("Cookie").Single(); client.DefaultRequestHeaders.Remove("Cookie"); client.DefaultRequestHeaders.Add("Cookie", session + "; " + cookie);
        var name = "MVC-" + Guid.NewGuid().ToString("N");
        var input = new Dictionary<string, string> { { "Ten", name }, { "KhongDoiTra", "not-a-bool" }, { "__RequestVerificationToken", WebUtility.HtmlDecode(token) } };
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await client.PostAsync("/Categories/Create", new FormUrlEncodedContent(input))).StatusCode);
        Assert.Equal(0, Convert.ToInt32(await db.DocMotGiaTriAsync("SELECT COUNT(*) FROM danh_muc WHERE ten=@name", ("@name", name))));
        input["KhongDoiTra"] = "true";
        Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/Categories/Create", new FormUrlEncodedContent(input))).StatusCode);
        Assert.Equal(1, Convert.ToInt32(await db.DocMotGiaTriAsync("SELECT COUNT(*) FROM danh_muc WHERE ten=@name AND khong_doi_tra=TRUE", ("@name", name))));
    }
    internal static void AddXsrf(HttpClient client)
    {
        if (client.DefaultRequestHeaders.TryGetValues("Cookie", out var cookies))
        { var token = cookies.Single().Split('=', 2)[1]; client.DefaultRequestHeaders.TryAddWithoutValidation(ThongTinXacThuc.HeaderXsrf, TokenPhien.TaoXsrf(token)); }
    }
}
