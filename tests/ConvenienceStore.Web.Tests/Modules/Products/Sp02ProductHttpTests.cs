using System.Net;
using System.Text.Json;
using ConvenienceStore.Web.Modules.Inventory;
using ConvenienceStore.Web.Modules.Products;
using ConvenienceStore.Web.Tests.HaTang;

namespace ConvenienceStore.Web.Tests.Modules.Products;

public sealed class Sp02ProductHttpTests(CsdlMySqlTam db) : IClassFixture<CsdlMySqlTam>
{
    [Theory]
    [InlineData(null, 401)]
    [InlineData("FORGED", 401)]
    [InlineData("LOCKED", 401)]
    [InlineData("EXPIRED", 401)]
    [InlineData("REVOKED", 401)]
    [InlineData("FORCED", 403)]
    public async Task SessionGuardAppliesBeforeDetailLookup(string? actor, int status)
    {
        await using var app = await ListFeatureHost.Start(db, s => { s.AddModuleProducts(); s.AddModuleInventory(); });
        using var client = await ListFeatureHost.Client(app, db, actor);
        Assert.Equal(status, (int)(await client.GetAsync("/api/v1/products/1")).StatusCode);
        Assert.Equal(status, (int)(await client.GetAsync("/Products/1")).StatusCode);
        Assert.Equal(status, (int)(await client.GetAsync("/api/v1/products/1/prices")).StatusCode);
    }

    [Theory]
    [InlineData("AD", true)]
    [InlineData("NK", false)]
    [InlineData("TN", false)]
    public async Task DetailAndHistoryRespectRoleAndRenderSafely(string role, bool admin)
    {
        var ids = await ProductReadSeed.Create(db);
        await db.ChayAsync("""
            UPDATE san_pham SET mo_ta='<img src=x onerror=alert(2)>',anh_url='javascript:alert(3)' WHERE id=@id;
            INSERT INTO lich_su_gia(san_pham_id,gia_ban,hieu_luc_tu,nguoi_tao_id,huy_luc,nguoi_huy_id)
            SELECT id,12300,NOW()+INTERVAL 2 DAY,nguoi_tao_id,NOW(),nguoi_tao_id FROM san_pham WHERE id=@id;
            """, ("@id", ids.Active));
        await using var app = await ListFeatureHost.Start(db, s => { s.AddModuleProducts(); s.AddModuleInventory(); });
        using var client = await ListFeatureHost.Client(app, db, role);
        using var response = await client.GetAsync($"/api/v1/products/{ids.Active}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); Assert.True(response.Headers.CacheControl!.NoStore);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); var item = json.RootElement;
        Assert.Equal(ids.Active, item.GetProperty("id").GetUInt32()); Assert.Equal(10000, item.GetProperty("giaBan").GetInt32());
        Assert.Equal(7, item.GetProperty("tonKho").GetInt32()); Assert.Equal(5, item.GetProperty("nguongToiThieu").GetInt32());
        Assert.Equal(admin ? JsonValueKind.Number : JsonValueKind.Null, item.GetProperty("giaVon").ValueKind);
        Assert.Equal(admin ? JsonValueKind.Object : JsonValueKind.Null, item.GetProperty("lichSuGia").ValueKind);
        var scheduled = item.GetProperty("giaDaLenLich"); Assert.Equal(1, scheduled.GetProperty("total").GetInt32());
        Assert.Equal(99900, scheduled.GetProperty("items")[0].GetProperty("giaBan").GetInt32());
        Assert.EndsWith("+07:00", scheduled.GetProperty("items")[0].GetProperty("hieuLucTu").GetString());
        Assert.Equal(JsonValueKind.Null, item.GetProperty("anhUrl").ValueKind);
        Assert.EndsWith("+07:00", item.GetProperty("ngayCapNhat").GetString());
        var html = await client.GetStringAsync($"/Products/{ids.Active}");
        Assert.Contains("&lt;img", html); Assert.DoesNotContain("<img src=x", html); Assert.DoesNotContain("javascript:", html);
        Assert.Equal(admin, html.Contains("data-price-history")); Assert.Equal(admin, html.Contains("data-cost"));
        Assert.DoesNotContain(">API ", html);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/Products/{ids.Active}/Prices")).StatusCode);
        Assert.Equal(admin ? HttpStatusCode.OK : HttpStatusCode.Forbidden, (await client.GetAsync($"/Products/{ids.Active}/Prices?daLenLich=false")).StatusCode);
        if (admin) {
            var historyPage = await client.GetStringAsync($"/Products/{ids.Active}/Prices?daLenLich=false&size=1");
            Assert.Contains("rel=\"next\"", historyPage); Assert.Contains("daLenLich=false", historyPage);
        }
        var list = await client.GetStringAsync($"/Products?danhMucId={ids.Category}"); Assert.Contains($"href=\"/Products/{ids.Active}\"", list);
        using var history = await client.GetAsync($"/api/v1/products/{ids.Active}/prices?daLenLich=false&size=1");
        Assert.Equal(admin ? HttpStatusCode.OK : HttpStatusCode.Forbidden, history.StatusCode);
        if (admin) { using var h = JsonDocument.Parse(await history.Content.ReadAsStringAsync()); Assert.Equal(3, h.RootElement.GetProperty("total").GetInt32()); Assert.Single(h.RootElement.GetProperty("items").EnumerateArray()); }
        Assert.Equal(role == "TN" ? HttpStatusCode.NotFound : HttpStatusCode.OK, (await client.GetAsync($"/api/v1/products/{ids.Inactive}")).StatusCode);
        Assert.Equal(role == "TN" ? HttpStatusCode.NotFound : HttpStatusCode.OK, (await client.GetAsync($"/Products/{ids.Inactive}")).StatusCode);
    }

    [Fact]
    public async Task MissingProductAndPricePaginationHaveExplicitContracts()
    {
        var ids = await ProductReadSeed.Create(db);
        await using var app = await ListFeatureHost.Start(db, s => { s.AddModuleProducts(); s.AddModuleInventory(); });
        using var client = await ListFeatureHost.Client(app, db, "AD");
        using var missing = await client.GetAsync("/api/v1/products/4294967295"); Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        using var error = JsonDocument.Parse(await missing.Content.ReadAsStringAsync()); Assert.Equal("Sản phẩm không còn tồn tại", error.RootElement.GetProperty("message").GetString());
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/Products/4294967295")).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await client.GetAsync($"/api/v1/products/{ids.Active}/prices?page=0")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/v1/products/{ids.Active}/prices?daLenLich=not-bool")).StatusCode);
        using var empty = JsonDocument.Parse(await client.GetStringAsync($"/api/v1/products/{ids.Active}/prices?page=2&size=1"));
        Assert.Empty(empty.RootElement.GetProperty("items").EnumerateArray());
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/v1/products/4294967295/prices")).StatusCode);
    }
}
