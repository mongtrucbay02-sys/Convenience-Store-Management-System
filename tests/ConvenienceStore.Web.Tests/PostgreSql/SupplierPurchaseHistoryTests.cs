using System.Net;
using System.Data.Common;
using System.Text.RegularExpressions;
using ConvenienceStore.Web.Tests.HaTang;
using ConvenienceStore.Web.Modules.Inventory;
using ConvenienceStore.Web.Modules.Purchasing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ConvenienceStore.Web.Data;
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
        var total = lines.Sum(line => checked(line.Quantity * line.Price));
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

        await using var app = await ListFeatureHost.Start(database, Register);
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
        Assert.Equal("2026-11-10", Attr(html, milk, "data-latest-date"));
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

        var boundaries = await client.GetStringAsync($"/NhapHang/LichSuNcc?supplierId={seed.Supplier}&from=2026-11-01&to=2026-11-10");
        Assert.Equal("3", Attr(boundaries, "data-nh09-receipts", "data-nh09-receipts"));
        var fromOnly = await client.GetStringAsync($"/NhapHang/LichSuNcc?supplierId={seed.Supplier}&from=2026-11-05");
        Assert.Equal("3", Attr(fromOnly, "data-nh09-receipts", "data-nh09-receipts"));
        var toOnly = await client.GetStringAsync($"/NhapHang/LichSuNcc?supplierId={seed.Supplier}&to=2026-11-05");
        Assert.Equal("2", Attr(toOnly, "data-nh09-receipts", "data-nh09-receipts"));
    }

    [Fact]
    public async Task NH09_E1_SupplierWithoutConfirmedReceipts_ShowsEmptyMessage()
    {
        var seed = await SeedAsync();
        await Receipt(seed, "D", seed.Empty, "2026-11-02", "NHAP", (seed.Milk, 1, 10000));
        await Receipt(seed, "V", seed.Empty, "2026-11-02", "DA_HUY", (seed.Milk, 1, 10000));
        await Receipt(seed, "O", seed.Empty, "2026-10-31", "DA_NHAP_KHO", (seed.Milk, 1, 10000));
        await using var app = await ListFeatureHost.Start(database, Register);
        using var client = await ListFeatureHost.Client(app, database, "AD");
        var response = await client.GetAsync($"/NhapHang/LichSuNcc?supplierId={seed.Empty}&from=2026-11-01&to=2026-11-30");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("data-nh09-empty", html);
        Assert.DoesNotContain("data-nh09-product", html);
        Assert.DoesNotContain("data-nh09-receipt=", html);
    }

    [Fact]
    public async Task NH09_WithoutSupplier_ShowsPickerOnly_AndUnknownSupplierIsNotFound()
    {
        await using var app = await ListFeatureHost.Start(database, Register);
        using var client = await ListFeatureHost.Client(app, database, "AD");
        var picker = await client.GetAsync("/NhapHang/LichSuNcc");
        Assert.Equal(HttpStatusCode.OK, picker.StatusCode);
        var html = await picker.Content.ReadAsStringAsync();
        Assert.Contains("name=\"supplierId\"", html);
        Assert.DoesNotContain("data-nh09-empty", html);
        Assert.DoesNotContain("data-nh09-total", html);
        Assert.DoesNotContain("data-nh09-product", html);
        Assert.DoesNotContain("data-nh09-receipt=", html);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/NhapHang/LichSuNcc?supplierId=9000000000001")).StatusCode);
    }

    [Theory]
    [InlineData("/NhapHang/LichSuNcc?supplierId=0")]
    [InlineData("/NhapHang/LichSuNcc?supplierId=-5")]
    [InlineData("/NhapHang/LichSuNcc?supplierId=abc")]
    [InlineData("/NhapHang/LichSuNcc?from=2026-11-03&to=2026-11-01")]
    [InlineData("/NhapHang/LichSuNcc?from=bad-date")]
    [InlineData("/NhapHang/LichSuNcc?to=bad-date")]
    [InlineData("/NhapHang/LichSuNcc?from=2026-02-30")]
    [InlineData("/NhapHang/LichSuNcc?from=11%2F01%2F2026")]
    [InlineData("/NhapHang/LichSuNcc?supplierId=9223372036854775808")]
    [InlineData("/NhapHang/LichSuNcc?page=0")]
    [InlineData("/NhapHang/LichSuNcc?page=100001")]
    [InlineData("/NhapHang/LichSuNcc?page=bad")]
    public async Task NH09_InvalidFiltersAreRejected(string path)
    {
        await using var app = await ListFeatureHost.Start(database, Register);
        using var client = await ListFeatureHost.Client(app, database, "AD");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(path)).StatusCode);
    }

    [Theory]
    [InlineData("AD", 200)]
    [InlineData("NK", 403)]
    [InlineData("TN", 403)]
    [InlineData("FORCED", 403)]
    [InlineData(null, 401)]
    [InlineData("FORGED", 401)]
    [InlineData("EXPIRED", 401)]
    [InlineData("REVOKED", 401)]
    [InlineData("LOCKED", 401)]
    public async Task NH09_OnlyAdminWithSupplierHistoryPermission(string? role, int expected)
    {
        await using var app = await ListFeatureHost.Start(database, Register);
        using var client = await ListFeatureHost.Client(app, database, role);
        Assert.Equal(expected, (int)(await client.GetAsync("/NhapHang/LichSuNcc")).StatusCode);
        if (expected != 200)
            Assert.Equal(expected, (int)(await client.GetAsync("/NhapHang/LichSuNcc?supplierId=9000000000001")).StatusCode);
    }

    [Fact]
    public async Task NH09_RanksByFrequencyThenQuantity_AndUsesLatestReceiptSnapshotWithStableTies()
    {
        var seed = await SeedAsync();
        await Receipt(seed, "FIRST", seed.Supplier, "2026-11-05", "DA_NHAP_KHO", (seed.Milk, 1, 20000));
        await Receipt(seed, "SECOND", seed.Supplier, "2026-11-05", "DA_NHAP_KHO", (seed.Milk, 1, 25000));
        await Receipt(seed, "THIRD", seed.Supplier, "2026-11-05", "DA_NHAP_KHO", (seed.Milk, 1, 21000));
        await database.ChayAsync("UPDATE phieu_nhap SET xac_nhan_luc='2026-11-05T08:00:00Z' WHERE ma_phieu IN (@second,@third)",
            ("@second", seed.Prefix + "SECOND"), ("@third", seed.Prefix + "THIRD"));
        await database.ChayAsync("UPDATE phieu_nhap SET xac_nhan_luc='2026-11-05T09:00:00Z' WHERE ma_phieu=@first", ("@first", seed.Prefix + "FIRST"));
        await Receipt(seed, "BREAD", seed.Supplier, "2026-11-06", "DA_NHAP_KHO", (seed.Bread, 100, 8000));
        await database.ChayAsync("UPDATE san_pham SET ten_san_pham='RENAMED',don_vi_tinh='NEW' WHERE id=@id", ("@id", seed.Milk));

        await using var app = await ListFeatureHost.Start(database, Register);
        using var client = await ListFeatureHost.Client(app, database, "AD");
        var path = $"/NhapHang/LichSuNcc?supplierId={seed.Supplier}";
        var html = await client.GetStringAsync(path);
        var milk = $"data-nh09-product=\"{seed.Milk}\"";
        var bread = $"data-nh09-product=\"{seed.Bread}\"";
        Assert.True(html.IndexOf(milk, StringComparison.Ordinal) < html.IndexOf(bread, StringComparison.Ordinal));
        Assert.Equal("20000", Attr(html, milk, "data-latest"));
        Assert.DoesNotContain("RENAMED", html);
        Assert.True(html.IndexOf(seed.Prefix + "THIRD\"", StringComparison.Ordinal) < html.IndexOf(seed.Prefix + "SECOND\"", StringComparison.Ordinal));
        await database.ChayAsync("UPDATE phieu_nhap SET xac_nhan_luc='2026-11-05T08:00:00Z' WHERE ma_phieu=@first", ("@first", seed.Prefix + "FIRST"));
        html = await client.GetStringAsync(path);
        Assert.Equal("21000", Attr(html, milk, "data-latest"));
        await Receipt(seed, "LATEST", seed.Supplier, "2026-11-07", "DA_NHAP_KHO", (seed.Milk, 1, 23000));
        html = await client.GetStringAsync(path);
        Assert.Equal("23000", Attr(html, milk, "data-latest"));
        Assert.Equal("2026-11-07", Attr(html, milk, "data-latest-date"));

        var tied = await client.GetStringAsync(path + "&from=2026-11-06");
        Assert.True(tied.IndexOf(bread, StringComparison.Ordinal) < tied.IndexOf(milk, StringComparison.Ordinal));
    }

    [Fact]
    public async Task NH09_ReceiptPagingPreservesFiltersAndSummaries_AndLinksToDetails()
    {
        var seed = await SeedAsync();
        for (var i = 0; i < 51; i++)
            await Receipt(seed, i.ToString("D2"), seed.Supplier, "2026-11-05", "DA_NHAP_KHO", (seed.Milk, 1, 50000));
        await Receipt(seed, "OUT", seed.Supplier, "2026-12-01", "DA_NHAP_KHO", (seed.Milk, 1, 1));
        await using var app = await ListFeatureHost.Start(database, Register);
        using var client = await ListFeatureHost.Client(app, database, "AD");
        var html = await client.GetStringAsync($"/NhapHang/LichSuNcc?supplierId={seed.Supplier}&from=2026-11-01&to=2026-11-30");
        Assert.Equal(50, Regex.Matches(html, "data-nh09-receipt=").Count);
        Assert.Equal("51", Attr(html, "data-nh09-receipts", "data-nh09-receipts"));
        Assert.Equal("2550000", Attr(html, "data-nh09-total", "data-nh09-total"));
        var nextLink = Regex.Matches(html, "href=\"([^\"]+)\"")
            .Select(m => WebUtility.HtmlDecode(m.Groups[1].Value)).Single(h => h.Contains("page=2", StringComparison.Ordinal));
        Assert.Contains($"supplierId={seed.Supplier}", nextLink);
        Assert.Contains("from=2026-11-01", nextLink);
        Assert.Contains("to=2026-11-30", nextLink);
        var next = await client.GetStringAsync(nextLink);
        Assert.Single(Regex.Matches(next, "data-nh09-receipt="));
        Assert.Contains(seed.Prefix + "00\"", next);
        Assert.DoesNotContain(seed.Prefix + "OUT\"", next);
        Assert.Equal("2550000", Attr(next, "data-nh09-total", "data-nh09-total"));
        Assert.Equal("51", Attr(next, $"data-nh09-product=\"{seed.Milk}\"", "data-receipts"));
        Assert.DoesNotContain("page=3", next);
        var details = Regex.Matches(next, "href=\"([^\"]+)\"[^>]*>" + seed.Prefix + "00</a>").Single();
        Assert.Matches("^/NhapHang/[0-9]+$", details.Groups[1].Value);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(details.Groups[1].Value)).StatusCode);
    }

    [Fact]
    public async Task NH09_AllProductsAreVisible_AndMoneyUsesInt64WithVietnameseFormatting()
    {
        var seed = await SeedAsync();
        await Receipt(seed, "BIG", seed.Supplier, "2026-11-05", "DA_NHAP_KHO", (seed.Milk, 100000, 50000));
        for (var i = 0; i < 21; i++)
        {
            var product = Convert.ToInt64(await database.DocMotGiaTriAsync("""
                INSERT INTO san_pham(ma,ten_san_pham,danh_muc_id,don_vi_tinh,nguoi_tao_id)
                SELECT @code,@code,danh_muc_id,'Hộp',@actor FROM san_pham WHERE id=@milk RETURNING id
                """, ("@code", seed.Prefix + "P" + i), ("@actor", seed.Actor), ("@milk", seed.Milk)));
            await Receipt(seed, "P" + i, seed.Supplier, "2026-11-05", "DA_NHAP_KHO", (product, 1, 1));
        }
        await using var app = await ListFeatureHost.Start(database, Register);
        using var client = await ListFeatureHost.Client(app, database, "AD");
        var html = await client.GetStringAsync($"/NhapHang/LichSuNcc?supplierId={seed.Supplier}");
        Assert.Equal(22, Regex.Matches(html, "data-nh09-product=").Count);
        Assert.Equal("5000000021", Attr(html, "data-nh09-total", "data-nh09-total"));
        Assert.Equal("5000000000", Attr(html, $"data-nh09-product=\"{seed.Milk}\"", "data-value"));
        Assert.Contains("5.000.000.021", html);
        Assert.Contains("table-responsive smartmart-data-table", html);
    }

    [Theory]
    [InlineData("AD", true)]
    [InlineData("NK", false)]
    public async Task NH09_MenuAndPurchasingButtonFollowPermissionClaim(string role, bool visible)
    {
        await using var app = await ListFeatureHost.Start(database, Register);
        using var client = await ListFeatureHost.Client(app, database, role);
        var html = await client.GetStringAsync("/NhapHang");
        Assert.Equal(visible ? 2 : 0, Regex.Matches(html, "href=\"/NhapHang/LichSuNcc\"").Count);
    }

    private static void Register(IServiceCollection services)
    {
        services.AddModuleInventory();
        services.AddScoped<PurchaseReceiptService>();
        services.AddScoped<SupplierPurchaseHistoryService>();
    }

    [Fact]
    public async Task NH09_ServiceUsesFourQueriesWithSqlDateAndStatusFilters_WithoutNPlusOne()
    {
        var seed = await SeedAsync();
        await Receipt(seed, "A", seed.Supplier, "2026-11-05", "DA_NHAP_KHO", (seed.Milk, 2, 20000), (seed.Bread, 1, 8000));
        await Receipt(seed, "B", seed.Supplier, "2026-11-06", "DA_NHAP_KHO", (seed.Milk, 3, 21000));
        var queries = new QueryRecorder();
        var options = new DbContextOptionsBuilder<CuaHangDbContext>()
            .UseNpgsql(database.ChuoiKetNoi, pg => pg.SetPostgresVersion(17, 0)).AddInterceptors(queries).Options;
        await using var db = new CuaHangDbContext(options);
        var model = await new SupplierPurchaseHistoryService(db).LoadAsync(seed.Supplier,
            new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30), 1, CancellationToken.None);
        Assert.NotNull(model);
        Assert.Equal(2, model.Products.Count);
        Assert.Equal(4, queries.Commands.Count);
        Assert.All(queries.Commands.Skip(1), sql =>
        {
            Assert.Contains("WHERE", sql);
            Assert.Contains("trang_thai", sql);
            Assert.Contains("ngay_nhap", sql);
            Assert.Contains(">=", sql);
            Assert.Contains("<=", sql);
        });
    }

    [Fact]
    public async Task NH09_TotalOverflowThrows_InsteadOfWrappingMoney()
    {
        var seed = await SeedAsync();
        var price = long.MaxValue / 2 + 1;
        await Receipt(seed, "A", seed.Supplier, "2026-11-05", "DA_NHAP_KHO", (seed.Milk, 1, price));
        await Receipt(seed, "B", seed.Supplier, "2026-11-06", "DA_NHAP_KHO", (seed.Milk, 1, price));
        await using var db = database.TaoDbContext();
        await Assert.ThrowsAsync<OverflowException>(() => new SupplierPurchaseHistoryService(db)
            .LoadAsync(seed.Supplier, null, null, 1, CancellationToken.None));
    }

    private sealed class QueryRecorder : DbCommandInterceptor
    {
        public List<string> Commands { get; } = [];

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            return ValueTask.FromResult(result);
        }
    }
}
