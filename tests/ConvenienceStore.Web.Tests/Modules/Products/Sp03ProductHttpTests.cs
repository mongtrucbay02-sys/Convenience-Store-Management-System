using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ConvenienceStore.Web.Modules.Inventory;
using ConvenienceStore.Web.Modules.Products;
using ConvenienceStore.Web.Tests.HaTang;

namespace ConvenienceStore.Web.Tests.Modules.Products;

public sealed class Sp03ProductHttpTests(CsdlMySqlTam db) : IClassFixture<CsdlMySqlTam>
{
    [Theory]
    [InlineData(null, 401)]
    [InlineData("TN", 403)]
    [InlineData("FORCED", 403)]
    [InlineData("LOCKED", 401)]
    [InlineData("EXPIRED", 401)]
    [InlineData("REVOKED", 401)]
    public async Task RealSessionBlocksUnauthorizedCreation(string? actor, int status)
    {
        await using var app = await ListFeatureHost.Start(db, s => { s.AddModuleProducts(); s.AddModuleInventory(); });
        using var client = await ListFeatureHost.Client(app, db, actor); Sp08CategoryHttpTests.AddXsrf(client);
        Assert.Equal(status, (int)(await client.PostAsJsonAsync("/api/v1/products", new { tenSanPham = "Forbidden" })).StatusCode);
        Assert.Equal(status, (int)(await client.GetAsync("/Products/Create")).StatusCode);
    }

    [Theory]
    [InlineData("AD")]
    [InlineData("NK")]
    public async Task CreateReadBackAndDuplicateCodeRespectInitialPriceOnly(string role)
    {
        var ids = await ProductReadSeed.Create(db);
        await using var app = await ListFeatureHost.Start(db, s => { s.AddModuleProducts(); s.AddModuleInventory(); });
        using var client = await ListFeatureHost.Client(app, db, role); Sp08CategoryHttpTests.AddXsrf(client);
        var code = "NEW-" + Guid.NewGuid().ToString("N")[..12];
        var input = new { ma = code, tenSanPham = " New product ", danhMucId = ids.Category, donViTinh = " Hộp ", giaBan = 10000, nguongToiThieu = 3, moTa = "<script>test</script>", tonKho = 999, nguoiTaoId = 4294967295, trangThai = "NGUNG_KINH_DOANH", giaVon = 999 };
        using var response = await client.PostAsJsonAsync("/api/v1/products", input); Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); var row = json.RootElement; var id = row.GetProperty("id").GetInt64();
        Assert.Equal("/api/v1/products/" + id, response.Headers.Location!.OriginalString); Assert.Equal(0, row.GetProperty("tonKho").GetInt32());
        Assert.Equal(10000, row.GetProperty("giaBan").GetInt32()); Assert.Equal(3, row.GetProperty("nguongToiThieu").GetInt32());
        Assert.Equal("New product", row.GetProperty("tenSanPham").GetString()); Assert.Equal("Hộp", row.GetProperty("donViTinh").GetString());
        Assert.Equal("DANG_KINH_DOANH", row.GetProperty("trangThai").GetString()); Assert.Equal(1, row.GetProperty("phienBan").GetInt32());
        Assert.Equal(1, Convert.ToInt32(await db.DocMotGiaTriAsync("SELECT COUNT(*) FROM lich_su_gia WHERE san_pham_id=@id AND gia_ban=10000 AND hieu_luc_tu<=NOW()", ("@id", id))));
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/v1/products", input with { ma = code.ToUpperInvariant() })).StatusCode);
        using var generated = await client.PostAsJsonAsync("/api/v1/products", new { tenSanPham = "Generated", danhMucId = ids.Category, donViTinh = "Cái", giaBan = 100 });
        Assert.Equal(HttpStatusCode.Created, generated.StatusCode);
        using var auto = JsonDocument.Parse(await generated.Content.ReadAsStringAsync()); Assert.InRange(auto.RootElement.GetProperty("ma").GetString()!.Length, 1, 20); Assert.Equal(5, auto.RootElement.GetProperty("nguongToiThieu").GetInt32());
        var html = await client.GetStringAsync("/Products/Create"); Assert.Contains("__RequestVerificationToken", html); Assert.Contains("name=\"TenSanPham\"", html);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/Products/Create", new FormUrlEncodedContent(new Dictionary<string,string> { ["TenSanPham"] = "No token" }))).StatusCode);
    }

    [Fact]
    public async Task ValidationHiddenCategoryCsrfAndDuplicateRaceAreAtomic()
    {
        var ids = await ProductReadSeed.Create(db);
        await using var app = await ListFeatureHost.Start(db, s => { s.AddModuleProducts(); s.AddModuleInventory(); });
        using var client = await ListFeatureHost.Client(app, db, "AD");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/v1/products", new { })).StatusCode); Sp08CategoryHttpTests.AddXsrf(client);
        using var invalid = await client.PostAsJsonAsync("/api/v1/products", new { tenSanPham = "", danhMucId = 0, donViTinh = "", giaBan = -1, nguongToiThieu = -1 });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode);
        using var errors = JsonDocument.Parse(await invalid.Content.ReadAsStringAsync()); Assert.True(errors.RootElement.GetProperty("details").GetArrayLength() >= 5);
        var body = new { ma = "RACE-" + Guid.NewGuid().ToString("N")[..12], tenSanPham = "Concurrent", danhMucId = ids.Category, donViTinh = "Cái", giaBan = 100 };
        using var first = await ListFeatureHost.Client(app, db, "AD"); Sp08CategoryHttpTests.AddXsrf(first);
        var responses = await Task.WhenAll(client.PostAsJsonAsync("/api/v1/products", body), first.PostAsJsonAsync("/api/v1/products", body));
        Assert.Equal(new[] { 201, 409 }, responses.Select(x => (int)x.StatusCode).Order().ToArray()); foreach(var response in responses) response.Dispose();
        await db.ChayAsync("UPDATE danh_muc SET trang_thai='DA_AN' WHERE id=@id", ("@id", ids.Category));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await client.PostAsJsonAsync("/api/v1/products", body with { ma = "HIDDEN-" + Guid.NewGuid().ToString("N")[..10] })).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await client.PostAsJsonAsync("/api/v1/products", body with { danhMucId = 4294967295L })).StatusCode);
    }

    [Theory]
    [InlineData(null, 422)]
    [InlineData("0", 422)]
    [InlineData("-100", 422)]
    [InlineData("101", 422)]
    [InlineData("100.5", 400)]
    [InlineData("4294967300", 422)]
    public async Task MonetaryValidationNeverCreatesInvalidPrice(string? value, int status)
    {
        var ids = await ProductReadSeed.Create(db);
        await using var app = await ListFeatureHost.Start(db, s => { s.AddModuleProducts(); s.AddModuleInventory(); });
        using var client = await ListFeatureHost.Client(app, db, "NK"); Sp08CategoryHttpTests.AddXsrf(client);
        var body = "{\"tenSanPham\":\"Bad price\",\"donViTinh\":\"Unit\",\"danhMucId\":" + ids.Category + ",\"giaBan\":" + (value ?? "null") + "}";
        Assert.Equal(status, (int)(await client.PostAsync("/api/v1/products", new StringContent(body, System.Text.Encoding.UTF8, "application/json"))).StatusCode);
    }

    [Fact]
    public async Task RazorMultipartFormUsesAntiforgeryBindingAndReusableCompactFields()
    {
        var ids = await ProductReadSeed.Create(db);
        await using var app = await ListFeatureHost.Start(db, s => { s.AddModuleProducts(); s.AddModuleInventory(); });
        using var client = await ListFeatureHost.Client(app, db, "NK");
        using var form = await client.GetAsync("/Products/Create"); var html = await form.Content.ReadAsStringAsync();
        var token = System.Text.RegularExpressions.Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value; Assert.NotEmpty(token);
        var cookie = form.Headers.GetValues("Set-Cookie").First().Split(';')[0]; var session = client.DefaultRequestHeaders.GetValues("Cookie").Single();
        client.DefaultRequestHeaders.Remove("Cookie"); client.DefaultRequestHeaders.Add("Cookie", session + "; " + cookie);
        var name = "MVC-SP03-" + Guid.NewGuid().ToString("N");
        MultipartFormDataContent Body(string price, bool badImage) {
            var body = new MultipartFormDataContent();
            foreach (var pair in new Dictionary<string,string> { ["TenSanPham"] = name, ["DanhMucId"] = ids.Category.ToString(), ["DonViTinh"] = "Hộp", ["GiaBan"] = price, ["__RequestVerificationToken"] = WebUtility.HtmlDecode(token) }) body.Add(new StringContent(pair.Value), pair.Key);
            var file = new ByteArrayContent(badImage ? "<script>evil</script>"u8.ToArray() : Sp03ImageHttpTests.Png); file.Headers.ContentType = new("image/png"); body.Add(file, "Anh", "unsafe.exe"); return body;
        }
        using (var invalid = Body("not-a-number", false)) Assert.Equal(HttpStatusCode.UnprocessableEntity, (await client.PostAsync("/Products/Create", invalid)).StatusCode);
        using (var invalid = Body("10000", true)) Assert.Equal(HttpStatusCode.UnprocessableEntity, (await client.PostAsync("/Products/Create", invalid)).StatusCode);
        Assert.Equal(0, Convert.ToInt32(await db.DocMotGiaTriAsync("SELECT COUNT(*) FROM san_pham WHERE ten_san_pham=@name", ("@name", name))));
        using (var valid = Body("10000", false)) Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/Products/Create", valid)).StatusCode);
        var url = Convert.ToString(await db.DocMotGiaTriAsync("SELECT anh_url FROM san_pham WHERE ten_san_pham=@name", ("@name", name)))!;
        Assert.StartsWith("/uploads/products/", url);
        Assert.Equal(1, Convert.ToInt32(await db.DocMotGiaTriAsync("SELECT phien_ban FROM san_pham WHERE ten_san_pham=@name", ("@name", name))));
        var path = Path.Combine(app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot"), url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        Assert.Equal(Sp03ImageHttpTests.Png, await File.ReadAllBytesAsync(path)); File.Delete(path);
        var compact = await client.GetStringAsync("/Products/Create?compact=true"); Assert.Contains("name=\"TenSanPham\"", compact); Assert.DoesNotContain("type=\"file\"", compact);
        // NH-04 host is absent on this branch: no claim that a receipt row was created.
    }
}
