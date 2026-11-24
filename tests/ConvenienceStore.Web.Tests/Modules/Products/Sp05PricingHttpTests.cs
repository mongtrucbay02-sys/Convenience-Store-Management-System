using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ConvenienceStore.Web.Modules.Inventory;
using ConvenienceStore.Web.Modules.Products;
using ConvenienceStore.Web.Tests.HaTang;

namespace ConvenienceStore.Web.Tests.Modules.Products;

public sealed class Sp05PricingHttpTests(CsdlPostgreSqlTam db) : IClassFixture<CsdlPostgreSqlTam>
{
    [Theory]
    [InlineData(null, 401)]
    [InlineData("TN", 403)]
    [InlineData("NK", 403)]
    [InlineData("FORCED", 403)]
    [InlineData("LOCKED", 401)]
    [InlineData("EXPIRED", 401)]
    [InlineData("REVOKED", 401)]
    public async Task OnlyRealAdminSessionCanChangeOrCancelPrice(string? actor, int status)
    {
        await using var app = await ListFeatureHost.Start(db, s => { s.AddModuleProducts(); s.AddModuleInventory(); });
        using var client = await ListFeatureHost.Client(app, db, actor); Sp08CategoryHttpTests.AddXsrf(client);
        Assert.Equal(status, (int)(await client.PostAsJsonAsync("/api/v1/products/1/prices", new { giaBan = 10000, phienBan = 1 })).StatusCode);
        Assert.Equal(status, (int)(await client.PostAsJsonAsync("/api/v1/products/1/prices/1/cancel", new { phienBan = 1 })).StatusCode);
        Assert.Equal(status, (int)(await client.GetAsync("/Products/1/Price")).StatusCode);
    }

    [Fact]
    public async Task ScheduleCancelAndImmediatePriceKeepAuditAndCurrentContract()
    {
        var ids = await ProductReadSeed.Create(db);
        await using var app = await ListFeatureHost.Start(db, s => { s.AddModuleProducts(); s.AddModuleInventory(); });
        using var client = await ListFeatureHost.Client(app, db, "AD"); Sp08CategoryHttpTests.AddXsrf(client);
        using var scheduled = await client.PostAsJsonAsync($"/api/v1/products/{ids.Active}/prices", new { giaBan = 11000, hieuLucTu = DateTimeOffset.UtcNow.AddDays(2), phienBan = 1, nguoiTaoId = 4294967295L });
        Assert.Equal(HttpStatusCode.Created, scheduled.StatusCode); using var schedule = JsonDocument.Parse(await scheduled.Content.ReadAsStringAsync());
        var priceId = schedule.RootElement.GetProperty("id").GetInt64(); Assert.Equal(2, schedule.RootElement.GetProperty("phienBan").GetInt32()); Assert.Equal(11000, schedule.RootElement.GetProperty("giaBan").GetInt32());
        Assert.EndsWith("+07:00", schedule.RootElement.GetProperty("hieuLucTu").GetString()); Assert.NotEqual(4294967295L, schedule.RootElement.GetProperty("nguoiTaoId").GetInt64());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(scheduled.Headers.Location)).StatusCode);
        using (var detail = JsonDocument.Parse(await client.GetStringAsync($"/api/v1/products/{ids.Active}"))) Assert.Equal(10000, detail.RootElement.GetProperty("giaBan").GetInt32());
        using var cancelled = await client.PostAsJsonAsync($"/api/v1/products/{ids.Active}/prices/{priceId}/cancel", new { phienBan = 2 }); Assert.Equal(HttpStatusCode.OK, cancelled.StatusCode);
        using var c = JsonDocument.Parse(await cancelled.Content.ReadAsStringAsync()); Assert.Equal(3, c.RootElement.GetProperty("phienBan").GetInt32()); Assert.Equal(JsonValueKind.String, c.RootElement.GetProperty("huyLuc").ValueKind); Assert.Equal(JsonValueKind.Number, c.RootElement.GetProperty("nguoiHuyId").ValueKind);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/v1/products/{ids.Active}/prices/{priceId}/cancel", new { phienBan = 3 })).StatusCode);
        using var immediate = await client.PostAsJsonAsync($"/api/v1/products/{ids.Active}/prices", new { giaBan = 12000, phienBan = 3 }); Assert.Equal(HttpStatusCode.Created, immediate.StatusCode);
        using var current = JsonDocument.Parse(await client.GetStringAsync($"/api/v1/products/{ids.Active}")); Assert.Equal(12000, current.RootElement.GetProperty("giaBan").GetInt32());
        var html = await client.GetStringAsync($"/Products/{ids.Active}/Price"); Assert.Contains("__RequestVerificationToken", html); Assert.Contains("name=\"GiaBan\"", html); Assert.Contains("Giá vốn", html);
        Assert.Contains($"href=\"/Products/{ids.Active}/Price\"", await client.GetStringAsync($"/Products/{ids.Active}"));
    }

    [Fact]
    public async Task BelowCostRequiresExplicitConfirmationAndStaleRetryCannotAppend()
    {
        var ids = await ProductReadSeed.Create(db);
        await using var app = await ListFeatureHost.Start(db, s => { s.AddModuleProducts(); s.AddModuleInventory(); });
        using var client = await ListFeatureHost.Client(app, db, "AD"); Sp08CategoryHttpTests.AddXsrf(client);
        var body = new { giaBan = 5000, phienBan = 1, xacNhanDuoiGiaVon = false };
        using var warning = await client.PostAsJsonAsync($"/api/v1/products/{ids.Active}/prices", body); Assert.Equal(HttpStatusCode.Conflict, warning.StatusCode);
        using var w = JsonDocument.Parse(await warning.Content.ReadAsStringAsync()); Assert.Equal("CAN_XAC_NHAN_GIA_DUOI_VON", w.RootElement.GetProperty("code").GetString());
        Assert.Equal(1, Convert.ToInt32(await db.DocMotGiaTriAsync("SELECT phien_ban FROM san_pham WHERE id=@id", ("@id", ids.Active))));
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync($"/api/v1/products/{ids.Active}/prices", body with { xacNhanDuoiGiaVon = true })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/v1/products/{ids.Active}/prices", body with { xacNhanDuoiGiaVon = true })).StatusCode);
        Assert.Equal(1, Convert.ToInt32(await db.DocMotGiaTriAsync("SELECT COUNT(*) FROM lich_su_gia WHERE san_pham_id=@id AND gia_ban=5000", ("@id", ids.Active))));
    }

    [Fact]
    public async Task InvalidPastInactiveMissingCrossProductAndCsrfAreRejected()
    {
        var ids = await ProductReadSeed.Create(db);
        await using var app = await ListFeatureHost.Start(db, s => { s.AddModuleProducts(); s.AddModuleInventory(); });
        using var client = await ListFeatureHost.Client(app, db, "AD");
        var body = new { giaBan = 10000, phienBan = 1, hieuLucTu = (string?)null };
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync($"/api/v1/products/{ids.Active}/prices", body)).StatusCode); Sp08CategoryHttpTests.AddXsrf(client);
        foreach(var price in new[] { -100, 0, 101 }) Assert.Equal(HttpStatusCode.UnprocessableEntity, (await client.PostAsJsonAsync($"/api/v1/products/{ids.Active}/prices", body with { giaBan = price })).StatusCode);
        foreach(var date in new[] { DateTimeOffset.UtcNow.AddHours(-1).ToString("O"), "2026-02-31T12:00:00+07:00", "2026-11-17T12:00:00" }) Assert.Equal(HttpStatusCode.UnprocessableEntity, (await client.PostAsJsonAsync($"/api/v1/products/{ids.Active}/prices", body with { hieuLucTu = date })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/v1/products/{ids.Inactive}/prices", body)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/v1/products/4294967295/prices", body)).StatusCode);
        var currentPrice = Convert.ToUInt32(await db.DocMotGiaTriAsync("SELECT lich_su_gia_id FROM v_gia_ban_hien_hanh WHERE san_pham_id=@id", ("@id", ids.Active)));
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/v1/products/{ids.Active}/prices/{currentPrice}/cancel", new { phienBan = 1 })).StatusCode);
        var otherPrice = Convert.ToUInt32(await db.DocMotGiaTriAsync("SELECT lich_su_gia_id FROM v_gia_ban_hien_hanh WHERE san_pham_id=@id", ("@id", ids.Inactive)));
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync($"/api/v1/products/{ids.Active}/prices/{otherPrice}/cancel", new { phienBan = 1 })).StatusCode);
    }

    [Theory]
    [InlineData("\"not-a-number\"", 400)]
    [InlineData("100.5", 400)]
    [InlineData("null", 422)]
    [InlineData("4294967300", 422)]
    public async Task InvalidNumericInputReturnsCommonError(string value, int status)
    {
        await using var app = await ListFeatureHost.Start(db, s => { s.AddModuleProducts(); s.AddModuleInventory(); });
        using var client = await ListFeatureHost.Client(app, db, "AD"); Sp08CategoryHttpTests.AddXsrf(client);
        using var response = await client.PostAsync("/api/v1/products/1/prices", new StringContent("{\"giaBan\":" + value + ",\"phienBan\":1}", System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(status, (int)response.StatusCode); using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); Assert.True(json.RootElement.TryGetProperty("code", out _));
    }

    [Fact]
    public async Task ConcurrentPriceRequestsCannotAppendTwice()
    {
        var ids = await ProductReadSeed.Create(db);
        await using var app = await ListFeatureHost.Start(db, s => { s.AddModuleProducts(); s.AddModuleInventory(); });
        using var first = await ListFeatureHost.Client(app, db, "AD"); using var second = await ListFeatureHost.Client(app, db, "AD"); Sp08CategoryHttpTests.AddXsrf(first); Sp08CategoryHttpTests.AddXsrf(second);
        var body = new { giaBan = 11000, phienBan = 1 };
        var responses = await Task.WhenAll(first.PostAsJsonAsync($"/api/v1/products/{ids.Active}/prices", body), second.PostAsJsonAsync($"/api/v1/products/{ids.Active}/prices", body));
        Assert.Equal(new[] { 201, 409 }, responses.Select(x => (int)x.StatusCode).Order().ToArray()); foreach(var response in responses) response.Dispose();
        Assert.Equal(1, Convert.ToInt32(await db.DocMotGiaTriAsync("SELECT COUNT(*) FROM lich_su_gia WHERE san_pham_id=@id AND gia_ban=11000", ("@id", ids.Active))));
    }

    [Fact]
    public async Task RazorWarningConfirmationSchedulingAndCancellationUseAntiforgery()
    {
        var ids = await ProductReadSeed.Create(db);
        await using var app = await ListFeatureHost.Start(db, s => { s.AddModuleProducts(); s.AddModuleInventory(); });
        using var client = await ListFeatureHost.Client(app, db, "AD");
        using var form = await client.GetAsync($"/Products/{ids.Active}/Price"); var html = await form.Content.ReadAsStringAsync();
        static string Token(string text) => WebUtility.HtmlDecode(System.Text.RegularExpressions.Regex.Match(text, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
        var token = Token(html); Assert.NotEmpty(token); var cookie = form.Headers.GetValues("Set-Cookie").First().Split(';')[0]; var session = client.DefaultRequestHeaders.GetValues("Cookie").Single(); client.DefaultRequestHeaders.Remove("Cookie"); client.DefaultRequestHeaders.Add("Cookie", session + "; " + cookie);
        var body = new Dictionary<string,string> { ["GiaBan"] = "not-number", ["PhienBan"] = "1", ["__RequestVerificationToken"] = token };
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await client.PostAsync($"/Products/{ids.Active}/Price", new FormUrlEncodedContent(body))).StatusCode);
        body["GiaBan"] = "5000";
        using var warning = await client.PostAsync($"/Products/{ids.Active}/Price", new FormUrlEncodedContent(body)); Assert.Equal(HttpStatusCode.Conflict, warning.StatusCode); Assert.Contains("xác nhận", await warning.Content.ReadAsStringAsync());
        body["XacNhanDuoiGiaVon"] = "true"; Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync($"/Products/{ids.Active}/Price", new FormUrlEncodedContent(body))).StatusCode);
        body["GiaBan"] = "11000"; body["PhienBan"] = "2"; body["HieuLucTu"] = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).AddDays(2).ToString("yyyy-MM-ddTHH:mm:ss"); body["XacNhanDuoiGiaVon"] = "false";
        Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync($"/Products/{ids.Active}/Price", new FormUrlEncodedContent(body))).StatusCode);
        var priceId = Convert.ToUInt32(await db.DocMotGiaTriAsync("SELECT id FROM lich_su_gia WHERE san_pham_id=@id AND gia_ban=11000", ("@id", ids.Active)));
        var cancelHtml = await client.GetStringAsync($"/Products/{ids.Active}/Prices/{priceId}/Cancel"); Assert.Equal(3, Convert.ToInt32(await db.DocMotGiaTriAsync("SELECT phien_ban FROM san_pham WHERE id=@id", ("@id", ids.Active))));
        Assert.Null(await db.DocMotGiaTriAsync("SELECT huy_luc FROM lich_su_gia WHERE id=@id", ("@id", priceId)));
        var cancel = new Dictionary<string,string> { ["PhienBan"] = "3", ["__RequestVerificationToken"] = Token(cancelHtml) };
        Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync($"/Products/{ids.Active}/Prices/{priceId}/Cancel", new FormUrlEncodedContent(cancel))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync($"/Products/{ids.Active}/Prices/{priceId}/Cancel", new FormUrlEncodedContent(cancel))).StatusCode);
    }
}
