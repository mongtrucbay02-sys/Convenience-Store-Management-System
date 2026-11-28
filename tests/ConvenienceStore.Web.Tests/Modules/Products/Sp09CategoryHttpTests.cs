using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ConvenienceStore.Web.Modules.Inventory;
using ConvenienceStore.Web.Modules.Products;
using ConvenienceStore.Web.Tests.HaTang;

namespace ConvenienceStore.Web.Tests.Modules.Products;

public sealed class Sp09CategoryHttpTests(CsdlPostgreSqlTam db) : IClassFixture<CsdlPostgreSqlTam>
{
    [Theory]
    [InlineData(null, 401)]
    [InlineData("TN", 403)]
    [InlineData("NK", 403)]
    [InlineData("FORCED", 403)]
    [InlineData("LOCKED", 401)]
    [InlineData("EXPIRED", 401)]
    [InlineData("REVOKED", 401)]
    public async Task RemovalAndHideRequireRealAdminSession(string? role, int status)
    {
        await using var app = await ListFeatureHost.Start(db, s => { s.AddModuleProducts(); s.AddModuleInventory(); });
        using var client = await ListFeatureHost.Client(app, db, role); Sp08CategoryHttpTests.AddXsrf(client);
        Assert.Equal(status, (int)(await Delete(client, 1, new { phienBan = 1, xacNhan = true })).StatusCode);
        Assert.Equal(status, (int)(await client.PostAsJsonAsync("/api/v1/categories/1/hide", new { phienBan = 1, xacNhan = true })).StatusCode);
        Assert.Equal(status, (int)(await client.GetAsync("/Categories/1/Delete")).StatusCode);
        Assert.Equal(status, (int)(await client.GetAsync("/Categories/1/Hide")).StatusCode);
    }

    [Fact]
    public async Task EmptyCategoryRequiresConfirmationAndDeletesWithoutTouchingOthers()
    {
        await using var app = await ListFeatureHost.Start(db, s => { s.AddModuleProducts(); s.AddModuleInventory(); });
        using var client = await ListFeatureHost.Client(app, db, "AD"); Sp08CategoryHttpTests.AddXsrf(client);
        var id = await CreateCategory(client); var other = await CreateCategory(client);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Delete(client, id, new { phienBan = 1, xacNhan = false })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Delete(client, id, new { phienBan = 1, xacNhan = true })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/categories/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/categories/{other}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Delete(client, id, new { phienBan = 1, xacNhan = true })).StatusCode);
    }

    [Fact]
    public async Task ReferencedCategoryCannotDeleteButCanHideIdempotently()
    {
        var ids = await ProductReadSeed.Create(db);
        await using var app = await ListFeatureHost.Start(db, s => { s.AddModuleProducts(); s.AddModuleInventory(); });
        using var client = await ListFeatureHost.Client(app, db, "AD"); Sp08CategoryHttpTests.AddXsrf(client);
        var body = new { phienBan = 1, xacNhan = true };
        using var blocked = await Delete(client, ids.Category, body); Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        using var error = JsonDocument.Parse(await blocked.Content.ReadAsStringAsync()); Assert.Contains("chuyển", error.RootElement.GetProperty("message").GetString()!.ToLowerInvariant());
        using var hidden = await client.PostAsJsonAsync($"/api/v1/categories/{ids.Category}/hide", body); Assert.Equal(HttpStatusCode.OK, hidden.StatusCode);
        using var j = JsonDocument.Parse(await hidden.Content.ReadAsStringAsync()); Assert.Equal("DA_AN", j.RootElement.GetProperty("trangThai").GetString()); Assert.Equal(2, j.RootElement.GetProperty("phienBan").GetInt32()); Assert.Equal(2, j.RootElement.GetProperty("soSanPham").GetInt32());
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/v1/categories/{ids.Category}/hide", body)).StatusCode);
        using var again = await client.PostAsJsonAsync($"/api/v1/categories/{ids.Category}/hide", body with { phienBan = 2 }); Assert.Equal(HttpStatusCode.OK, again.StatusCode);
        using var unchanged = JsonDocument.Parse(await again.Content.ReadAsStringAsync()); Assert.Equal(2, unchanged.RootElement.GetProperty("phienBan").GetInt32());
        Assert.Equal(2, Convert.ToInt32(await db.DocMotGiaTriAsync("SELECT COUNT(*) FROM san_pham WHERE danh_muc_id=@id", ("@id", ids.Category))));
        var html = await client.GetStringAsync("/Categories"); Assert.Contains($"href=\"/Categories/{ids.Category}/Delete\"", html);
    }

    [Fact]
    public async Task MoveThroughExistingSp04MakesCategoryEmptyWithoutCascade()
    {
        var ids = await ProductReadSeed.Create(db);
        await using var app = await ListFeatureHost.Start(db, s => { s.AddModuleProducts(); s.AddModuleInventory(); });
        using var client = await ListFeatureHost.Client(app, db, "AD"); Sp08CategoryHttpTests.AddXsrf(client);
        var target = await CreateCategory(client);
        foreach (var product in new[] { ids.Active, ids.Inactive }) {
            using var detail = JsonDocument.Parse(await client.GetStringAsync($"/api/v1/products/{product}")); var row = detail.RootElement;
            var input = new { tenSanPham = row.GetProperty("tenSanPham").GetString(), danhMucId = target, donViTinh = row.GetProperty("donViTinh").GetString(), nguongToiThieu = row.GetProperty("nguongToiThieu").GetInt32(), phienBan = row.GetProperty("phienBan").GetInt64() };
            Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/v1/products/{product}", input)).StatusCode);
            if (product == ids.Active) Assert.Equal(HttpStatusCode.Conflict, (await Delete(client, ids.Category, new { phienBan = 1, xacNhan = true })).StatusCode); // Inactive reference still blocks.
        }
        Assert.Equal(HttpStatusCode.NoContent, (await Delete(client, ids.Category, new { phienBan = 1, xacNhan = true })).StatusCode);
        Assert.Equal(2, Convert.ToInt32(await db.DocMotGiaTriAsync("SELECT COUNT(*) FROM san_pham WHERE danh_muc_id=@target", ("@target", target))));
    }

    [Fact]
    public async Task XsrfMissingVersionMalformedAndMissingResourcesAreSafe()
    {
        await using var app = await ListFeatureHost.Start(db, s => { s.AddModuleProducts(); s.AddModuleInventory(); });
        using var client = await ListFeatureHost.Client(app, db, "AD");
        Assert.Equal(HttpStatusCode.Forbidden, (await Delete(client, 1, new { phienBan = 1, xacNhan = true })).StatusCode); Sp08CategoryHttpTests.AddXsrf(client);
        var id = await CreateCategory(client);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Delete(client, id, new { phienBan = 0, xacNhan = true })).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await client.PostAsJsonAsync($"/api/v1/categories/{id}/hide", new { phienBan = 1, xacNhan = false })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/v1/categories/4294967295/hide", new { phienBan = 1, xacNhan = true })).StatusCode);
        using var malformed = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/categories/{id}") { Content = new StringContent("{\"phienBan\":\"bad\",\"xacNhan\":true}", System.Text.Encoding.UTF8, "application/json") };
        Assert.Equal(HttpStatusCode.BadRequest, (await client.SendAsync(malformed)).StatusCode);
    }

    internal static async Task<long> CreateCategory(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/categories", new { ten = "SP09-" + Guid.NewGuid().ToString("N") }); Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); return json.RootElement.GetProperty("id").GetInt64();
    }
    internal static async Task<HttpResponseMessage> Delete(HttpClient client, long id, object body)
    { using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/categories/{id}") { Content = JsonContent.Create(body) }; return await client.SendAsync(request); }

    [Fact]
    public async Task RazorConfirmationShowsAlternativesAndRequiresAntiforgery()
    {
        var ids = await ProductReadSeed.Create(db);
        await using var app = await ListFeatureHost.Start(db, s => { s.AddModuleProducts(); s.AddModuleInventory(); });
        using var client = await ListFeatureHost.Client(app, db, "AD"); Sp08CategoryHttpTests.AddXsrf(client);
        var referenced = await client.GetStringAsync($"/Categories/{ids.Category}/Delete"); Assert.Contains($"href=\"/Products?danhMucId={ids.Category}\"", referenced); Assert.Contains($"href=\"/Categories/{ids.Category}/Hide\"", referenced);
        var id = await CreateCategory(client);
        using var form = await client.GetAsync($"/Categories/{id}/Delete"); var html = await form.Content.ReadAsStringAsync();
        var token = WebUtility.HtmlDecode(System.Text.RegularExpressions.Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value); Assert.NotEmpty(token);
        var cookie = form.Headers.GetValues("Set-Cookie").First().Split(';')[0]; var session = client.DefaultRequestHeaders.GetValues("Cookie").Single(); client.DefaultRequestHeaders.Remove("Cookie"); client.DefaultRequestHeaders.Add("Cookie", session + "; " + cookie);
        var body = new Dictionary<string,string> { ["PhienBan"] = "1", ["XacNhan"] = "true" };
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync($"/Categories/{id}/Delete", new FormUrlEncodedContent(body))).StatusCode);
        body["__RequestVerificationToken"] = token; body["XacNhan"] = "false";
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await client.PostAsync($"/Categories/{id}/Delete", new FormUrlEncodedContent(body))).StatusCode);
        body["XacNhan"] = "true"; Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync($"/Categories/{id}/Delete", new FormUrlEncodedContent(body))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/categories/{id}")).StatusCode);
    }
}
