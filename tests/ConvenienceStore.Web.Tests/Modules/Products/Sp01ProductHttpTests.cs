using System.Net;
using System.Text.Json;
using ConvenienceStore.Web.Modules.Inventory;
using ConvenienceStore.Web.Modules.Products;
using ConvenienceStore.Web.Tests.HaTang;

namespace ConvenienceStore.Web.Tests.Modules.Products;

public sealed class Sp01ProductHttpTests(CsdlPostgreSqlTam db) : IClassFixture<CsdlPostgreSqlTam>
{
    [Theory]
    [InlineData(null, 401)]
    [InlineData("FORGED", 401)]
    [InlineData("LOCKED", 401)]
    [InlineData("EXPIRED", 401)]
    [InlineData("REVOKED", 401)]
    [InlineData("FORCED", 403)]
    [InlineData("AD", 200)]
    [InlineData("NK", 200)]
    [InlineData("TN", 200)]
    public async Task RealSessionEnforcesListAccess(string? actor, int status)
    {
        await using var app = await ListFeatureHost.Start(db, s => { s.AddModuleProducts(); s.AddModuleInventory(); });
        using var client = await ListFeatureHost.Client(app, db, actor);
        Assert.Equal(status, (int)(await client.GetAsync("/api/v1/products")).StatusCode);
        Assert.Equal(status, (int)(await client.GetAsync("/Products")).StatusCode);
    }

    [Theory]
    [InlineData("AD", 2, true)]
    [InlineData("NK", 2, false)]
    [InlineData("TN", 1, false)]
    public async Task SearchFiltersStockAndRoleProjectionUsePostgreSql(string role, int count, bool seesCost)
    {
        var ids = await ProductReadSeed.Create(db);
        await using var app = await ListFeatureHost.Start(db, s => { s.AddModuleProducts(); s.AddModuleInventory(); });
        using var client = await ListFeatureHost.Client(app, db, role);
        using var json = JsonDocument.Parse(await client.GetStringAsync($"/api/v1/products?keyword=sUa&danhMucId={ids.Category}&sort=-giaBan,tenSanPham"));
        var list = json.RootElement.GetProperty("items");
        Assert.Equal(count, json.RootElement.GetProperty("total").GetInt32());
        Assert.Equal(count, list.GetArrayLength());
        Assert.All(list.EnumerateArray(), row => {
            Assert.Equal(7, row.GetProperty("tonKho").GetInt32());
            Assert.Equal(seesCost ? JsonValueKind.Number : JsonValueKind.Null, row.GetProperty("giaVon").ValueKind);
            Assert.Equal(ids.Category, row.GetProperty("danhMuc").GetProperty("id").GetInt64());
        });
        Assert.Equal(role == "TN" ? 10000 : 20000, list[0].GetProperty("giaBan").GetInt32());
        using var bounded = JsonDocument.Parse(await client.GetStringAsync($"/api/v1/products?danhMucId={ids.Category}&giaBanTu=10000&giaBanDen=10000&size=999"));
        Assert.Equal(1, bounded.RootElement.GetProperty("total").GetInt32());
        Assert.Equal(100, bounded.RootElement.GetProperty("size").GetInt32());
        var html = await client.GetStringAsync($"/Products?danhMucId={ids.Category}");
        Assert.Contains("name=\"keyword\"", html);
        Assert.Contains("<datalist id=\"product-categories\">", html);
        Assert.Contains($"value=\"{ids.Category}\"", html);
        Assert.Contains("&lt;script&gt;", html);
        Assert.DoesNotContain("<script>alert", html);
        Assert.Equal(seesCost, html.Contains("data-cost"));
        Assert.Contains("Không tìm thấy sản phẩm", await client.GetStringAsync("/Products?keyword=NEVER_MATCH_" + Guid.NewGuid()));
    }

    [Theory]
    [InlineData("page=0", 422)]
    [InlineData("page=2147483647&size=100", 422)]
    [InlineData("size=-1", 422)]
    [InlineData("page=nope", 400)]
    [InlineData("trangThai=INVALID", 422)]
    [InlineData("giaBanTu=-1", 422)]
    [InlineData("giaBanTu=200&giaBanDen=100", 422)]
    [InlineData("sort=giaVon", 422)]
    [InlineData("sort=tenSanPham;DROP TABLE san_pham", 422)]
    [InlineData("unknownFilter=1", 422)]
    public async Task InvalidQueryUsesSafeErrorContract(string query, int status)
    {
        await using var app = await ListFeatureHost.Start(db, s => { s.AddModuleProducts(); s.AddModuleInventory(); });
        using var client = await ListFeatureHost.Client(app, db, "AD");
        using var response = await client.GetAsync("/api/v1/products?" + query);
        Assert.Equal(status, (int)response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.TryGetProperty("code", out _));
        Assert.True(json.RootElement.TryGetProperty("traceId", out _));
        Assert.DoesNotContain("stackTrace", await response.Content.ReadAsStringAsync());
    }
}

internal static class ProductReadSeed
{
    public static async Task<(long Category, long Active, long Inactive)> Create(CsdlPostgreSqlTam db)
    {
        var key = Guid.NewGuid().ToString("N");
        await db.ChayAsync("""
            INSERT INTO tai_khoan(ten_dang_nhap,mat_khau_bam,ho_ten,vai_tro_id)
            SELECT @key,'unused','Product test',id FROM vai_tro WHERE ma='AD';
            INSERT INTO danh_muc(ten) VALUES(@key);
            INSERT INTO san_pham(ma,ten_san_pham,danh_muc_id,don_vi_tinh,nguoi_tao_id)
            SELECT LEFT(@key,18),'Sữa <script>alert(1)</script>',d.id,'Hộp',t.id
            FROM danh_muc d JOIN tai_khoan t ON t.ten_dang_nhap=@key WHERE d.ten=@key;
            INSERT INTO san_pham(ma,ten_san_pham,danh_muc_id,don_vi_tinh,nguoi_tao_id,trang_thai)
            SELECT CONCAT(LEFT(@key,18),'x'),'SỮA inactive',d.id,'Hộp',t.id,'NGUNG_KINH_DOANH'
            FROM danh_muc d JOIN tai_khoan t ON t.ten_dang_nhap=@key WHERE d.ten=@key;
            INSERT INTO ton_kho(san_pham_id,so_luong_ton,gia_von_binh_quan,nguong_toi_thieu)
            SELECT p.id,7,(CASE WHEN p.trang_thai='DANG_KINH_DOANH' THEN 8000 ELSE 12000 END),5
            FROM san_pham p JOIN danh_muc d ON p.danh_muc_id=d.id WHERE d.ten=@key;
            INSERT INTO lich_su_gia(san_pham_id,gia_ban,hieu_luc_tu,nguoi_tao_id)
            SELECT p.id,(CASE WHEN p.trang_thai='DANG_KINH_DOANH' THEN 10000 ELSE 20000 END),NOW()-INTERVAL '1 day',p.nguoi_tao_id
            FROM san_pham p JOIN danh_muc d ON p.danh_muc_id=d.id WHERE d.ten=@key;
            INSERT INTO lich_su_gia(san_pham_id,gia_ban,hieu_luc_tu,nguoi_tao_id)
            SELECT p.id,99900,NOW()+INTERVAL '1 day',p.nguoi_tao_id
            FROM san_pham p JOIN danh_muc d ON p.danh_muc_id=d.id WHERE d.ten=@key AND p.trang_thai='DANG_KINH_DOANH';
            """, ("@key", key));
        var cat = Convert.ToUInt32(await db.DocMotGiaTriAsync("SELECT id FROM danh_muc WHERE ten=@key", ("@key", key)));
        var active = Convert.ToUInt32(await db.DocMotGiaTriAsync("SELECT id FROM san_pham WHERE danh_muc_id=@id AND trang_thai='DANG_KINH_DOANH'", ("@id", cat)));
        var inactive = Convert.ToUInt32(await db.DocMotGiaTriAsync("SELECT id FROM san_pham WHERE danh_muc_id=@id AND trang_thai='NGUNG_KINH_DOANH'", ("@id", cat)));
        return (cat, active, inactive);
    }
}
