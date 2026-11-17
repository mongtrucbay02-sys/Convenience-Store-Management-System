using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Modules.Products.Services;
using ConvenienceStore.Web.Tests.HaTang;

namespace ConvenienceStore.Web.Tests.Modules.Products;

public sealed class CategoryMySqlTests(CsdlMySqlTam database) : IClassFixture<CsdlMySqlTam>
{
    [Fact]
    public async Task StablePagesCountAllAssignedProductsAndDoNotTrackEntities()
    {
        await database.ChayAsync("""
            INSERT INTO tai_khoan (id,ten_dang_nhap,mat_khau_bam,ho_ten,vai_tro_id)
            SELECT 910001,'cat_test','unused','Category test',id FROM vai_tro WHERE ma='AD';
            INSERT INTO danh_muc (id,ten,trang_thai) VALUES (910001,'Visible','HOAT_DONG'),(910002,'Hidden','DA_AN'),(910003,'Empty','HOAT_DONG');
            INSERT INTO san_pham (ma,ten_san_pham,danh_muc_id,don_vi_tinh,nguoi_tao_id,trang_thai)
            VALUES ('cat-a','Product A',910001,'cai',910001,'DANG_KINH_DOANH'),('cat-b','Product B',910001,'cai',910001,'NGUNG_KINH_DOANH');
            """);
        await using var db = database.TaoDbContext();
        var repository = new EfCategoryReadRepository(db);
        var first = await repository.GetCategoriesAsync(new CategoryListQuery { Size = 1 });
        var second = await repository.GetCategoriesAsync(new CategoryListQuery { Page = 2, Size = 1 });
        Assert.Equal(3, first.Total);
        Assert.Equal(2, Assert.Single(first.Items).SoSanPham);
        Assert.Equal("DA_AN", Assert.Single(second.Items).TrangThai);
        Assert.Equal(910002u, second.Items[0].Id);
        var all = await repository.GetCategoriesAsync(new CategoryListQuery { Size = 101 });
        Assert.Equal(100, all.Size);
        Assert.Equal(0, all.Items[2].SoSanPham);
        Assert.Empty((await repository.GetCategoriesAsync(new CategoryListQuery { Page = 4, Size = 1 })).Items);
        Assert.Empty(db.ChangeTracker.Entries());
        using var cts = new CancellationTokenSource(); cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.GetCategoriesAsync(new CategoryListQuery(), cts.Token));
    }
}
