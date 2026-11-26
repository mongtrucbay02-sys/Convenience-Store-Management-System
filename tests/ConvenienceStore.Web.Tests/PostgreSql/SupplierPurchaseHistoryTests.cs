using System.Net;
using System.Text.RegularExpressions;
using ConvenienceStore.Web.Tests.HaTang;
namespace ConvenienceStore.Web.Tests.PostgreSql;

/// <summary>NH-09: lịch sử nhập theo nhà cung cấp, chạy HTTP thật trên PostgreSQL cô lập.</summary>
public sealed class SupplierPurchaseHistoryTests(CsdlPostgreSqlTam database) : IClassFixture<CsdlPostgreSqlTam>
{
    private sealed record Seed(long Supplier, long Other, long Empty, long Actor, long Milk, long Bread, string Prefix);

    private async Task<Seed> SeedAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var actor = Convert.ToInt64(await database.DocMotGiaTriAsync(
            "INSERT INTO tai_khoan(ten_dang_nhap,mat_khau_bam,ho_ten,vai_tro_id,bat_buoc_doi_mk) VALUES(@name,'unused','NH-09 test',1,FALSE) RETURNING id",
            ("@name", "nh09_" + suffix)));
        async Task<long> Supplier(string code, string name) => Convert.ToInt64(await database.DocMotGiaTriAsync(
            "INSERT INTO nha_cung_cap(ma,ten,so_dien_thoai) VALUES(@code,@name,@phone) RETURNING id",
            ("@code", code + suffix), ("@name", name + suffix), ("@phone", "0" + Random.Shared.Next(100000000, 999999999))));
        var category = Convert.ToInt64(await database.DocMotGiaTriAsync(
            "INSERT INTO danh_muc(ten) VALUES(@name) RETURNING id", ("@name", "NH09 " + suffix)));
        async Task<long> Product(string code, string name) => Convert.ToInt64(await database.DocMotGiaTriAsync(
            "INSERT INTO san_pham(ma,ten_san_pham,danh_muc_id,don_vi_tinh,nguoi_tao_id) VALUES(@code,@name,@category,'Hộp',@actor) RETURNING id",
            ("@code", code + suffix), ("@name", name + suffix), ("@category", category), ("@actor", actor)));
        return new(await Supplier("HA", "NCC lịch sử "), await Supplier("HB", "NCC khác "), await Supplier("HC", "NCC chưa nhập "),
            actor, await Product("SM", "Sữa "), await Product("BM", "Bánh "), "H9" + suffix);
    }

    private async Task Receipt(Seed seed, string code, long supplier, string date, string status,
        params (long Product, int Quantity, long Price)[] lines)
    {
        var total = lines.Sum(line => line.Quantity * line.Price);
        var id = Convert.ToInt64(await database.DocMotGiaTriAsync("""
            INSERT INTO phieu_nhap(ma_phieu,nha_cung_cap_id,ngay_nhap,trang_thai,tong_tien,nguoi_lap_id,
                nguoi_xac_nhan_id,xac_nhan_luc,nguoi_huy_id,huy_luc,ly_do_huy)
            VALUES(@code,@supplier,CAST(@date AS date),@status,@total,@actor,
                CASE WHEN @status='DA_NHAP_KHO' THEN @actor END, CASE WHEN @status='DA_NHAP_KHO' THEN NOW() END,
                CASE WHEN @status='DA_HUY' THEN @actor END, CASE WHEN @status='DA_HUY' THEN NOW() END,
                CASE WHEN @status='DA_HUY' THEN 'NH-09 test' END)
            RETURNING id
            """, ("@code", seed.Prefix + code), ("@supplier", supplier), ("@date", date), ("@status", status),
            ("@total", total), ("@actor", seed.Actor)));
        foreach (var line in lines)
            await database.ChayAsync("""
                INSERT INTO ct_phieu_nhap(phieu_nhap_id,san_pham_id,ten_san_pham,don_vi_tinh,so_luong,gia_nhap)
                SELECT @receipt,id,ten_san_pham,don_vi_tinh,@quantity,@price FROM san_pham WHERE id=@product
                """, ("@receipt", id), ("@product", line.Product), ("@quantity", line.Quantity), ("@price", line.Price));
    }

    private static string? Attr(string html, string marker, string attribute)
    {
        var row = Regex.Match(html, "<[^>]*" + Regex.Escape(marker) + "[^>]*>");
        if (!row.Success) return null;
        var value = Regex.Match(row.Value, attribute + "=\"([^\"]*)\"");
        return value.Success ? value.Groups[1].Value : null;
    }

    [Fact]
    public async Task NH09_ConfirmedReceiptsOnly_AggregatesTotalsTopProductsAndPriceRange()
    {
        var seed = await SeedAsync();
        await Receipt(seed, "A", seed.Supplier, "2026-11-01", "DA_NHAP_KHO", (seed.Milk, 10, 20000), (seed.Bread, 5, 8000));
        await Receipt(seed, "B", seed.Supplier, "2026-11-05", "DA_NHAP_KHO", (seed.Milk, 4, 22000));
        await Receipt(seed, "C", seed.Supplier, "2026-11-10", "DA_NHAP_KHO", (seed.Milk, 6, 19000));
        await Receipt(seed, "DRAFT", seed.Supplier, "2026-11-11", "NHAP", (seed.Milk, 99, 1000));
        await Receipt(seed, "VOID", seed.Supplier, "2026-11-12", "DA_HUY", (seed.Milk, 99, 99000));
        await Receipt(seed, "OUT", seed.Supplier, "2026-12-01", "DA_NHAP_KHO", (seed.Milk, 1, 50000));
        await Receipt(seed, "OTHER", seed.Other, "2026-11-03", "DA_NHAP_KHO", (seed.Milk, 1, 1000));

        await using var app = await ListFeatureHost.Start(database, _ => { });
        using var client = await ListFeatureHost.Client(app, database, "AD");
        var html = await client.GetStringAsync($"/NhapHang/LichSuNcc?supplierId={seed.Supplier}&from=2026-11-01&to=2026-11-30");

        foreach (var code in new[] { "A", "B", "C" }) Assert.Contains(seed.Prefix + code + "\"", html);
        foreach (var code in new[] { "DRAFT", "VOID", "OUT", "OTHER" }) Assert.DoesNotContain(seed.Prefix + code + "\"", html);
        Assert.Equal("3", Attr(html, "data-nh09-receipts", "data-nh09-receipts"));
        Assert.Equal((10 * 20000 + 5 * 8000 + 4 * 22000 + 6 * 19000).ToString(), Attr(html, "data-nh09-total", "data-nh09-total"));

        var milk = $"data-nh09-product=\"{seed.Milk}\"";
        Assert.Equal("3", Attr(html, milk, "data-receipts"));
        Assert.Equal("20", Attr(html, milk, "data-quantity"));
        Assert.Equal((10 * 20000 + 4 * 22000 + 6 * 19000).ToString(), Attr(html, milk, "data-value"));
        Assert.Equal("19000", Attr(html, milk, "data-min"));
        Assert.Equal("22000", Attr(html, milk, "data-max"));
        Assert.Equal("19000", Attr(html, milk, "data-latest"));
        var bread = $"data-nh09-product=\"{seed.Bread}\"";
        Assert.Equal("1", Attr(html, bread, "data-receipts"));
        Assert.Equal("8000", Attr(html, bread, "data-latest"));
        Assert.True(html.IndexOf(milk, StringComparison.Ordinal) < html.IndexOf(bread, StringComparison.Ordinal),
            "Mặt hàng nhập nhiều lần hơn phải đứng trước.");
        Assert.True(html.IndexOf(seed.Prefix + "C\"", StringComparison.Ordinal) < html.IndexOf(seed.Prefix + "A\"", StringComparison.Ordinal),
            "Phiếu mới nhất phải đứng trước.");

        var sameDay = await client.GetStringAsync($"/NhapHang/LichSuNcc?supplierId={seed.Supplier}&from=2026-11-05&to=2026-11-05");
        Assert.Equal("1", Attr(sameDay, "data-nh09-receipts", "data-nh09-receipts"));
        Assert.Equal("22000", Attr(sameDay, milk, "data-latest"));
    }

    [Fact]
    public async Task NH09_E1_SupplierWithoutConfirmedReceipts_ShowsEmptyMessage()
    {
        var seed = await SeedAsync();
        await Receipt(seed, "D", seed.Empty, "2026-11-02", "NHAP", (seed.Milk, 1, 10000));
        await using var app = await ListFeatureHost.Start(database, _ => { });
        using var client = await ListFeatureHost.Client(app, database, "AD");
        var response = await client.GetAsync($"/NhapHang/LichSuNcc?supplierId={seed.Empty}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("data-nh09-empty", html);
        Assert.DoesNotContain("data-nh09-product", html);
    }

    [Fact]
    public async Task NH09_WithoutSupplier_ShowsPickerOnly_AndUnknownSupplierIsNotFound()
    {
        await using var app = await ListFeatureHost.Start(database, _ => { });
        using var client = await ListFeatureHost.Client(app, database, "AD");
        var picker = await client.GetAsync("/NhapHang/LichSuNcc");
        Assert.Equal(HttpStatusCode.OK, picker.StatusCode);
        Assert.Contains("name=\"supplierId\"", await picker.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/NhapHang/LichSuNcc?supplierId=9000000000001")).StatusCode);
    }

    [Theory]
    [InlineData("/NhapHang/LichSuNcc?supplierId=0")]
    [InlineData("/NhapHang/LichSuNcc?supplierId=-5")]
    [InlineData("/NhapHang/LichSuNcc?supplierId=abc")]
    [InlineData("/NhapHang/LichSuNcc?from=2026-11-03&to=2026-11-01")]
    [InlineData("/NhapHang/LichSuNcc?from=bad-date")]
    public async Task NH09_InvalidFiltersAreRejected(string path)
    {
        await using var app = await ListFeatureHost.Start(database, _ => { });
        using var client = await ListFeatureHost.Client(app, database, "AD");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(path)).StatusCode);
    }

    [Theory]
    [InlineData("AD", 200)]
    [InlineData("NK", 403)]
    [InlineData("TN", 403)]
    [InlineData("FORCED", 403)]
    [InlineData(null, 401)]
    public async Task NH09_OnlyAdminWithSupplierHistoryPermission(string? role, int expected)
    {
        await using var app = await ListFeatureHost.Start(database, _ => { });
        using var client = await ListFeatureHost.Client(app, database, role);
        Assert.Equal(expected, (int)(await client.GetAsync("/NhapHang/LichSuNcc")).StatusCode);
    }
}
