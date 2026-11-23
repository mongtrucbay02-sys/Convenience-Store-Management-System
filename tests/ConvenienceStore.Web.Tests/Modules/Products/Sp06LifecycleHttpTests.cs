using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ConvenienceStore.Web.Modules.Inventory;
using ConvenienceStore.Web.Modules.Products;
using ConvenienceStore.Web.Tests.HaTang;

namespace ConvenienceStore.Web.Tests.Modules.Products;

public sealed class Sp06LifecycleHttpTests(CsdlMySqlTam db) : IClassFixture<CsdlMySqlTam>
{
    [Theory]
    [InlineData(null, 401)]
    [InlineData("TN", 403)]
    [InlineData("NK", 403)]
    [InlineData("FORCED", 403)]
    [InlineData("LOCKED", 401)]
    [InlineData("EXPIRED", 401)]
    [InlineData("REVOKED", 401)]
    public async Task LifecycleRequiresAdminSession(string? role, int status)
    {
        await using var app = await ListFeatureHost.Start(db, s => { s.AddModuleProducts(); s.AddModuleInventory(); });
        using var client = await ListFeatureHost.Client(app, db, role); Sp08CategoryHttpTests.AddXsrf(client);
        Assert.Equal(status, (int)(await client.GetAsync("/api/v1/products/1/lifecycle")).StatusCode);
        Assert.Equal(status, (int)(await client.PostAsJsonAsync("/api/v1/products/1/discontinue", new { phienBan = 1, xacNhan = true })).StatusCode);
        Assert.Equal(status, (int)(await Delete(client, 1, new { phienBan = 1, xacNhan = true })).StatusCode);
        Assert.Equal(status, (int)(await client.GetAsync("/Products/1/Discontinue")).StatusCode);
    }

    [Fact]
    public async Task StockWarningRequiresAdditionalConfirmationWithoutChangingQuantity()
    {
        var ids = await ProductReadSeed.Create(db);
        await using var app = await ListFeatureHost.Start(db, s => { s.AddModuleProducts(); s.AddModuleInventory(); });
        using var client = await ListFeatureHost.Client(app, db, "AD"); Sp08CategoryHttpTests.AddXsrf(client);
        var body = new { phienBan = 1, xacNhan = true, xacNhanTonKho = false };
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/v1/products/{ids.Active}/discontinue", body)).StatusCode);
        using var changed = await client.PostAsJsonAsync($"/api/v1/products/{ids.Active}/discontinue", body with { xacNhanTonKho = true }); Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        using var j = JsonDocument.Parse(await changed.Content.ReadAsStringAsync()); Assert.Equal("NGUNG_KINH_DOANH", j.RootElement.GetProperty("trangThai").GetString()); Assert.Equal(2, j.RootElement.GetProperty("phienBan").GetInt32());
        Assert.Equal(7, Convert.ToInt32(await db.DocMotGiaTriAsync("SELECT so_luong_ton FROM ton_kho WHERE san_pham_id=@id", ("@id", ids.Active))));
        using var active = JsonDocument.Parse(await client.GetStringAsync($"/api/v1/products?danhMucId={ids.Category}&trangThai=DANG_KINH_DOANH")); Assert.DoesNotContain(active.RootElement.GetProperty("items").EnumerateArray(), x => x.GetProperty("id").GetInt64() == ids.Active);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"/api/v1/products/{ids.Active}/reactivate", body with { phienBan = 2 })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/v1/products/{ids.Active}/reactivate", body with { phienBan = 3 })).StatusCode);
    }

    [Fact]
    public async Task ConfirmedUnusedDeleteRemovesOnlyOwnedConfiguration()
    {
        var ids = await ProductReferenceSeed.Unused(db);
        await using var app = await ListFeatureHost.Start(db, s => { s.AddModuleProducts(); s.AddModuleInventory(); });
        using var client = await ListFeatureHost.Client(app, db, "AD"); Sp08CategoryHttpTests.AddXsrf(client);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Delete(client, ids.Product, new { phienBan = 1, xacNhan = false })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Delete(client, ids.Product, new { phienBan = 1, xacNhan = true })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/products/{ids.Product}")).StatusCode);
        Assert.Equal(0, Convert.ToInt32(await db.DocMotGiaTriAsync("SELECT COUNT(*) FROM lich_su_gia WHERE san_pham_id=@id", ("@id", ids.Product))));
        Assert.Equal(0, Convert.ToInt32(await db.DocMotGiaTriAsync("SELECT COUNT(*) FROM ton_kho WHERE san_pham_id=@id", ("@id", ids.Product))));
    }

    [Fact]
    public async Task HeldOrdersAndDraftReceiptsWarnAndBlockPhysicalDeletion()
    {
        var ids = await ProductReferenceSeed.Unused(db); await ProductReferenceSeed.HeldAndDraft(db, ids.Product, ids.Actor);
        await using var app = await ListFeatureHost.Start(db, s => { s.AddModuleProducts(); s.AddModuleInventory(); });
        using var client = await ListFeatureHost.Client(app, db, "AD"); Sp08CategoryHttpTests.AddXsrf(client);
        using var preview = JsonDocument.Parse(await client.GetStringAsync($"/api/v1/products/{ids.Product}/lifecycle"));
        Assert.Equal(1, preview.RootElement.GetProperty("donTamGiu").GetInt32()); Assert.Equal(1, preview.RootElement.GetProperty("phieuNhapNhap").GetInt32()); Assert.True(preview.RootElement.GetProperty("coThamChieu").GetBoolean());
        var body = new { phienBan = 1, xacNhan = true };
        Assert.Equal(HttpStatusCode.Conflict, (await Delete(client, ids.Product, body)).StatusCode);
        using var changed = await client.PostAsJsonAsync($"/api/v1/products/{ids.Product}/discontinue", body); Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        using var result = JsonDocument.Parse(await changed.Content.ReadAsStringAsync()); Assert.Equal(1, result.RootElement.GetProperty("donTamGiu").GetInt32()); Assert.Equal(1, result.RootElement.GetProperty("phieuNhapNhap").GetInt32());
        Assert.Equal("Original receipt", Convert.ToString(await db.DocMotGiaTriAsync("SELECT ten_san_pham FROM ct_phieu_nhap WHERE san_pham_id=@id", ("@id", ids.Product))));
    }

    [Fact]
    public async Task XsrfStaleMissingAndDuplicateConcurrentWritesAreSafe()
    {
        var ids = await ProductReferenceSeed.Unused(db);
        await using var app = await ListFeatureHost.Start(db, s => { s.AddModuleProducts(); s.AddModuleInventory(); });
        using var first = await ListFeatureHost.Client(app, db, "AD"); var body = new { phienBan = 1, xacNhan = true };
        Assert.Equal(HttpStatusCode.Forbidden, (await first.PostAsJsonAsync($"/api/v1/products/{ids.Product}/discontinue", body)).StatusCode); Sp08CategoryHttpTests.AddXsrf(first);
        using var second = await ListFeatureHost.Client(app, db, "AD"); Sp08CategoryHttpTests.AddXsrf(second);
        var responses = await Task.WhenAll(first.PostAsJsonAsync($"/api/v1/products/{ids.Product}/discontinue", body), second.PostAsJsonAsync($"/api/v1/products/{ids.Product}/discontinue", body));
        Assert.Equal(new[] { 200, 409 }, responses.Select(x => (int)x.StatusCode).Order().ToArray()); foreach(var response in responses) response.Dispose();
        Assert.Equal(HttpStatusCode.NotFound, (await first.PostAsJsonAsync("/api/v1/products/4294967295/discontinue", body)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Delete(first, 4294967295L, body)).StatusCode);
    }

    internal static async Task<HttpResponseMessage> Delete(HttpClient client, long id, object body)
    { using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/products/{id}") { Content = JsonContent.Create(body) }; return await client.SendAsync(request); }

    [Fact]
    public async Task RazorConfirmationAndStockAcknowledgementUseRealAntiforgery()
    {
        var ids = await ProductReadSeed.Create(db);
        await using var app = await ListFeatureHost.Start(db, s => { s.AddModuleProducts(); s.AddModuleInventory(); });
        using var client = await ListFeatureHost.Client(app, db, "AD");
        using var form = await client.GetAsync($"/Products/{ids.Active}/Discontinue"); var html = await form.Content.ReadAsStringAsync();
        Assert.Contains("name=\"XacNhanTonKho\"", html); Assert.Contains("&lt;script&gt;", html); Assert.DoesNotContain("<script>alert", html);
        var token = WebUtility.HtmlDecode(System.Text.RegularExpressions.Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value); Assert.NotEmpty(token);
        var cookie = form.Headers.GetValues("Set-Cookie").First().Split(';')[0]; var session = client.DefaultRequestHeaders.GetValues("Cookie").Single(); client.DefaultRequestHeaders.Remove("Cookie"); client.DefaultRequestHeaders.Add("Cookie", session + "; " + cookie);
        var body = new Dictionary<string,string> { ["PhienBan"] = "1", ["XacNhan"] = "true" };
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync($"/Products/{ids.Active}/Discontinue", new FormUrlEncodedContent(body))).StatusCode);
        body["__RequestVerificationToken"] = token;
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync($"/Products/{ids.Active}/Discontinue", new FormUrlEncodedContent(body))).StatusCode);
        body["XacNhanTonKho"] = "true"; Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync($"/Products/{ids.Active}/Discontinue", new FormUrlEncodedContent(body))).StatusCode);
        body["PhienBan"] = "2"; Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync($"/Products/{ids.Active}/Reactivate", new FormUrlEncodedContent(body))).StatusCode);
        Assert.Equal(3, Convert.ToInt32(await db.DocMotGiaTriAsync("SELECT phien_ban FROM san_pham WHERE id=@id", ("@id", ids.Active))));
    }
}
