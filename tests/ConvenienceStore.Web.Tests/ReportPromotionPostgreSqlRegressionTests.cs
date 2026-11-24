using System.Security.Claims;
using ConvenienceStore.Web.Modules.Promotions.Models;
using ConvenienceStore.Web.Modules.Promotions.Services;
using ConvenienceStore.Web.Modules.Reports.Services;
using ConvenienceStore.Web.Tests.HaTang;
using Microsoft.AspNetCore.Http;

namespace ConvenienceStore.Web.Tests;

public sealed class ReportPromotionPostgreSqlRegressionTests(CsdlPostgreSqlTam database) : IClassFixture<CsdlPostgreSqlTam>
{
    [Theory]
    [InlineData(DiscountType.Percent, null)]
    [InlineData(DiscountType.Percent, 1000L)]
    [InlineData(DiscountType.FixedAmount, null)]
    public async Task PromotionCreateAndUpdateRespectNullableDiscountCap(DiscountType type, long? cap)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        await database.ChayAsync("""
            INSERT INTO tai_khoan(ten_dang_nhap,mat_khau_bam,ho_ten,vai_tro_id,bat_buoc_doi_mk)
            SELECT @name,'unused','Promotion test',id,FALSE FROM vai_tro WHERE ma='AD';
            INSERT INTO danh_muc(ten) VALUES(@name);
            """, ("@name", "km_" + suffix));
        var actor = Convert.ToInt64(await database.DocMotGiaTriAsync(
            "SELECT id FROM tai_khoan WHERE ten_dang_nhap=@name", ("@name", "km_" + suffix)));
        await database.ChayAsync("""
            INSERT INTO san_pham(ma,ten_san_pham,danh_muc_id,don_vi_tinh,nguoi_tao_id)
            SELECT @sku,'Promotion test product',id,'Hộp',@actor FROM danh_muc WHERE ten=@name
            """, ("@sku", "KM-" + suffix), ("@actor", actor), ("@name", "km_" + suffix));
        var product = Convert.ToInt64(await database.DocMotGiaTriAsync(
            "SELECT id FROM san_pham WHERE ma=@sku", ("@sku", "KM-" + suffix)));
        using var context = database.TaoDbContext();
        var http = new HttpContextAccessor { HttpContext = new DefaultHttpContext {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, actor.ToString())], "test"))
        } };
        var source = new PostgreSqlPromotionDataSource(context, http);
        var now = DateTimeOffset.UtcNow;
        var created = await source.CreateAsync(new PromotionRecord(0, "", "Test " + suffix, null,
            type, type == DiscountType.Percent ? 10 : 1000, cap,
            [new ProductRefDto(product, "KM-" + suffix, "Promotion test product")],
            now.AddDays(1), now.AddDays(2), false, PromotionStatus.Draft, 0), default);
        Assert.Equal(cap, created.MaxDiscountAmount);
        Assert.Equal(type, created.DiscountType);
        Assert.Single(created.Products);
        Assert.True(await source.UpdateAsync(created with { Name = "Updated " + suffix }, default));
        var loaded = await source.GetByIdAsync(created.Id, default);
        Assert.NotNull(loaded);
        Assert.Equal(cap, loaded.MaxDiscountAmount);
        Assert.Equal("Updated " + suffix, loaded.Name);
        var stored = await database.DocMotGiaTriAsync(
            "SELECT giam_toi_da FROM chuong_trinh_khuyen_mai WHERE id=@id", ("@id", created.Id));
        if (cap is null) Assert.True(stored is null or DBNull);
        else Assert.Equal(cap.Value, Convert.ToInt64(stored));
    }

    [Fact]
    public async Task InventoryValueUsesDecimalCostUntilTheTotalIsCalculated()
    {
        await database.ChayAsync("""
            INSERT INTO tai_khoan(ten_dang_nhap,mat_khau_bam,ho_ten,vai_tro_id,bat_buoc_doi_mk)
            SELECT 'cost_test','unused','Cost test',id,FALSE FROM vai_tro WHERE ma='AD';
            INSERT INTO danh_muc(ten) VALUES('Cost regression');
            INSERT INTO san_pham(ma,ten_san_pham,danh_muc_id,don_vi_tinh,nguoi_tao_id)
            SELECT 'COST-TEST','Cost regression',d.id,'Hộp',a.id
            FROM danh_muc d CROSS JOIN tai_khoan a WHERE d.ten='Cost regression' AND a.ten_dang_nhap='cost_test';
            INSERT INTO ton_kho(san_pham_id,so_luong_ton,gia_von_binh_quan)
            SELECT id,24,10416.67 FROM san_pham WHERE ma='COST-TEST';
            """);
        var product = Convert.ToInt64(await database.DocMotGiaTriAsync(
            "SELECT id FROM san_pham WHERE ma='COST-TEST'"));
        using var context = database.TaoDbContext();
        var source = new PostgreSqlTransactionalReportsDataSource(context);
        var products = await source.GetProductsAsync(default);
        Assert.Equal(10416.67m, products.Single(x => x.Id == product).UnitCost);
        var report = await new OperationalReportsService(source, TimeProvider.System)
            .GetInventoryAsync(new() { ProductId = product }, default);
        Assert.Equal(250000, report.Metrics.Single(x => x.Label == "Giá trị tồn").Value);
        Assert.Equal("250.000 ₫", report.Rows.Single(x => x.Key == $"product:{product}").Cells[4]);
        var total = report.Rows.Single(x => x.Key == "total");
        Assert.Equal(report.Columns.Count, total.Cells.Count);
        Assert.Equal("250.000 ₫", total.Cells[4]);
        Assert.Equal("24", total.Cells[9]);
    }

    [Fact]
    public async Task ReportFiltersUseStoredIdsAndKeepHistoricalEntriesSelectable()
    {
        var name = "Filter " + Guid.NewGuid().ToString("N")[..8];
        await database.ChayAsync("""
            INSERT INTO danh_muc(id,ten,trang_thai) VALUES(9001,@name,'DA_AN');
            INSERT INTO nha_cung_cap(id,ma,ten,so_dien_thoai,trang_thai)
            VALUES(9001,'FILTER-NCC',@name,'0900000777','NGUNG_HOP_TAC');
            """, ("@name", name));
        using var context = database.TaoDbContext();
        var filters = new ReportFilterService(context);
        var categories = await filters.GetCategoriesAsync(9001, default);
        var suppliers = await filters.GetSuppliersAsync(9001, default);
        Assert.Contains(categories, x => x.Value == "9001" && x.Text == name && x.Selected);
        Assert.Contains(suppliers, x => x.Value == "9001" && x.Text == name && x.Selected);
    }
}
