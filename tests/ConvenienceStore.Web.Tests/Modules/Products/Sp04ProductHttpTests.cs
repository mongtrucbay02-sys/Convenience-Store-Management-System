using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ConvenienceStore.Web.Modules.Inventory;
using ConvenienceStore.Web.Modules.Products;
using ConvenienceStore.Web.Tests.HaTang;

namespace ConvenienceStore.Web.Tests.Modules.Products;

public sealed class Sp04ProductHttpTests(CsdlMySqlTam db) : IClassFixture<CsdlMySqlTam>
{
    [Theory]
    [InlineData(null, 401)]
    [InlineData("TN", 403)]
    [InlineData("FORCED", 403)]
    [InlineData("LOCKED", 401)]
    [InlineData("EXPIRED", 401)]
    [InlineData("REVOKED", 401)]
    public async Task SessionAndPermissionGuardMetadataWrites(string? actor, int status)
    {
        await using var app = await ListFeatureHost.Start(db, s => { s.AddModuleProducts(); s.AddModuleInventory(); });
        using var client = await ListFeatureHost.Client(app, db, actor); Sp08CategoryHttpTests.AddXsrf(client);
        Assert.Equal(status, (int)(await client.PutAsJsonAsync("/api/v1/products/1", new { })).StatusCode);
        Assert.Equal(status, (int)(await client.GetAsync("/Products/1/Edit")).StatusCode);
    }

    [Theory]
    [InlineData("AD")]
    [InlineData("NK")]
    public async Task MetadataUpdatePersistsButCannotOverpostPriceStockOrCode(string role)
    {
        var ids = await ProductReadSeed.Create(db);
        await using var app = await ListFeatureHost.Start(db, s => { s.AddModuleProducts(); s.AddModuleInventory(); });
        using var client = await ListFeatureHost.Client(app, db, role); Sp08CategoryHttpTests.AddXsrf(client);
        using var original = JsonDocument.Parse(await client.GetStringAsync($"/api/v1/products/{ids.Active}")); var code = original.RootElement.GetProperty("ma").GetString();
        var input = new { tenSanPham = " Edited ", danhMucId = ids.Category, donViTinh = "Hộp", moTa = "<script>safe</script>", nguongToiThieu = 9, phienBan = 1, ma = "OVERPOST", giaBan = 99900, tonKho = 999, giaVon = 99, trangThai = "NGUNG_KINH_DOANH" };
        using var saved = await client.PutAsJsonAsync($"/api/v1/products/{ids.Active}", input); Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        using var json = JsonDocument.Parse(await saved.Content.ReadAsStringAsync()); var row = json.RootElement;
        Assert.Equal("Edited", row.GetProperty("tenSanPham").GetString()); Assert.Equal(2, row.GetProperty("phienBan").GetInt32());
        Assert.Equal(9, row.GetProperty("nguongToiThieu").GetInt32()); Assert.Equal(7, row.GetProperty("tonKho").GetInt32());
        Assert.Equal(10000, row.GetProperty("giaBan").GetInt32()); Assert.Equal(code, row.GetProperty("ma").GetString()); Assert.Equal("DANG_KINH_DOANH", row.GetProperty("trangThai").GetString());
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/v1/products/{ids.Active}", input)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/v1/products/{ids.Active}", input with { phienBan = 2, donViTinh = "Cái" })).StatusCode);
        var detail = await client.GetStringAsync($"/Products/{ids.Active}"); Assert.Contains($"href=\"/Products/{ids.Active}/Edit\"", detail); Assert.Contains("&lt;script&gt;", detail);
        var form = await client.GetStringAsync($"/Products/{ids.Active}/Edit"); Assert.Contains("__RequestVerificationToken", form); Assert.Contains("name=\"PhienBan\"", form); Assert.DoesNotContain("name=\"GiaBan\"", form);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync($"/Products/{ids.Active}/Edit", new FormUrlEncodedContent(new Dictionary<string,string> { ["TenSanPham"] = "CSRF" }))).StatusCode);
    }

    [Fact]
    public async Task EmptyStockAllowsUnitChangeUntilBusinessHistoryExists()
    {
        var ids = await ProductReadSeed.Create(db); await db.ChayAsync("UPDATE ton_kho SET so_luong_ton=0 WHERE san_pham_id=@id", ("@id", ids.Active));
        await using var app = await ListFeatureHost.Start(db, s => { s.AddModuleProducts(); s.AddModuleInventory(); });
        using var client = await ListFeatureHost.Client(app, db, "AD"); Sp08CategoryHttpTests.AddXsrf(client);
        var input = new { tenSanPham = "Unit test", danhMucId = ids.Category, donViTinh = "Cái", nguongToiThieu = 5, phienBan = 1 };
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/v1/products/{ids.Active}", input)).StatusCode);
        await db.ChayAsync("""
            INSERT INTO the_kho(san_pham_id,loai_bien_dong,ma_chung_tu,chung_tu_id,so_luong_thay_doi,ton_sau,gia_von_ap_dung,gia_von_sau,nguoi_thuc_hien_id)
            SELECT id,'BAN_HANG','SP04-SOLD',1,-7,0,8000,8000,nguoi_tao_id FROM san_pham WHERE id=@id;
            """, ("@id", ids.Active));
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/v1/products/{ids.Active}", input with { donViTinh = "Hộp", phienBan = 2 })).StatusCode);
    }

    [Fact]
    public async Task InvalidMissingAndXsrfRequestsNeverWrite()
    {
        var ids = await ProductReadSeed.Create(db);
        await using var app = await ListFeatureHost.Start(db, s => { s.AddModuleProducts(); s.AddModuleInventory(); });
        using var client = await ListFeatureHost.Client(app, db, "AD");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync($"/api/v1/products/{ids.Active}", new { })).StatusCode); Sp08CategoryHttpTests.AddXsrf(client);
        using var invalid = await client.PutAsJsonAsync($"/api/v1/products/{ids.Active}", new { tenSanPham = "", danhMucId = 0, donViTinh = "", nguongToiThieu = -1, phienBan = 0 }); Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode);
        using var error = JsonDocument.Parse(await invalid.Content.ReadAsStringAsync()); Assert.Equal(5, error.RootElement.GetProperty("details").GetArrayLength());
        var input = new { tenSanPham = "Valid", danhMucId = ids.Category, donViTinh = "Hộp", nguongToiThieu = 5, phienBan = 1 };
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync("/api/v1/products/4294967295", input)).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await client.PutAsJsonAsync($"/api/v1/products/{ids.Active}", input with { danhMucId = uint.MaxValue })).StatusCode);
        Assert.Equal(1, Convert.ToInt32(await db.DocMotGiaTriAsync("SELECT phien_ban FROM san_pham WHERE id=@id", ("@id", ids.Active))));
    }

    [Fact]
    public async Task ConcurrentHttpUpdatesReturnOneSuccessAndOneConflict()
    {
        var ids = await ProductReadSeed.Create(db);
        await using var app = await ListFeatureHost.Start(db, s => { s.AddModuleProducts(); s.AddModuleInventory(); });
        using var first = await ListFeatureHost.Client(app, db, "AD"); using var second = await ListFeatureHost.Client(app, db, "NK"); Sp08CategoryHttpTests.AddXsrf(first); Sp08CategoryHttpTests.AddXsrf(second);
        var input = new { tenSanPham = "Concurrent A", danhMucId = ids.Category, donViTinh = "Hộp", nguongToiThieu = 8, phienBan = 1 };
        var responses = await Task.WhenAll(first.PutAsJsonAsync($"/api/v1/products/{ids.Active}", input), second.PutAsJsonAsync($"/api/v1/products/{ids.Active}", input with { tenSanPham = "Concurrent B" }));
        Assert.Equal(new[] { 200, 409 }, responses.Select(x => (int)x.StatusCode).Order().ToArray()); foreach(var response in responses) response.Dispose();
    }

    [Fact]
    public async Task RazorEditSubmitsMetadataAndImageTogetherAndRejectsStaleRepeat()
    {
        var ids = await ProductReadSeed.Create(db);
        await using var app = await ListFeatureHost.Start(db, s => { s.AddModuleProducts(); s.AddModuleInventory(); });
        using var client = await ListFeatureHost.Client(app, db, "NK");
        using var form = await client.GetAsync($"/Products/{ids.Active}/Edit"); var html = await form.Content.ReadAsStringAsync();
        var token = System.Text.RegularExpressions.Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value; Assert.NotEmpty(token);
        var cookie = form.Headers.GetValues("Set-Cookie").First().Split(';')[0]; var session = client.DefaultRequestHeaders.GetValues("Cookie").Single(); client.DefaultRequestHeaders.Remove("Cookie"); client.DefaultRequestHeaders.Add("Cookie", session + "; " + cookie);
        MultipartFormDataContent Body(string version) {
            var content = new MultipartFormDataContent();
            foreach(var pair in new Dictionary<string,string> { ["TenSanPham"] = "Razor edited", ["DanhMucId"] = ids.Category.ToString(), ["DonViTinh"] = "Hộp", ["NguongToiThieu"] = "8", ["PhienBan"] = version, ["__RequestVerificationToken"] = WebUtility.HtmlDecode(token) }) content.Add(new StringContent(pair.Value), pair.Key);
            var file = new ByteArrayContent(Sp03ImageHttpTests.Png); file.Headers.ContentType = new("image/png"); content.Add(file, "Anh", "unsafe.exe"); return content;
        }
        using(var invalid = Body("bad-version")) Assert.Equal(HttpStatusCode.UnprocessableEntity, (await client.PostAsync($"/Products/{ids.Active}/Edit", invalid)).StatusCode);
        using(var valid = Body("1")) Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync($"/Products/{ids.Active}/Edit", valid)).StatusCode);
        using(var stale = Body("1")) {
            var response = await client.PostAsync($"/Products/{ids.Active}/Edit", stale); Assert.Equal(HttpStatusCode.Conflict, response.StatusCode); Assert.Contains("Tải lại dữ liệu", await response.Content.ReadAsStringAsync());
        }
        Assert.Equal(2, Convert.ToInt32(await db.DocMotGiaTriAsync("SELECT phien_ban FROM san_pham WHERE id=@id", ("@id", ids.Active))));
        var url = Convert.ToString(await db.DocMotGiaTriAsync("SELECT anh_url FROM san_pham WHERE id=@id", ("@id", ids.Active)))!;
        var path = Path.Combine(app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot"), url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)); Assert.Equal(Sp03ImageHttpTests.Png, await File.ReadAllBytesAsync(path)); File.Delete(path);
    }
}
