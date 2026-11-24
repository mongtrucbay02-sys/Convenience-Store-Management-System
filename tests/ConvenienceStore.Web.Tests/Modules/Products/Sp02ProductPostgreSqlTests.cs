using ConvenienceStore.Web.Modules.Products.Services;
using ConvenienceStore.Web.Tests.HaTang;

namespace ConvenienceStore.Web.Tests.Modules.Products;

public sealed class Sp02ProductPostgreSqlTests(CsdlPostgreSqlTam database) : IClassFixture<CsdlPostgreSqlTam>
{
    [Fact]
    public async Task CurrentPriceAndFutureHistoryRespectBoundaryCancellationAndPaging()
    {
        var ids = await ProductReadSeed.Create(database);
        await database.ChayAsync("""
            INSERT INTO lich_su_gia(san_pham_id,gia_ban,hieu_luc_tu,nguoi_tao_id)
            SELECT id,10100,NOW(),nguoi_tao_id FROM san_pham WHERE id=@id;
            INSERT INTO lich_su_gia(san_pham_id,gia_ban,hieu_luc_tu,nguoi_tao_id,huy_luc,nguoi_huy_id)
            SELECT id,88800,NOW()+INTERVAL '2 day',nguoi_tao_id,NOW(),nguoi_tao_id FROM san_pham WHERE id=@id;
            """, ("@id", ids.Active));
        await using var db = database.TaoDbContext(); var repo = new EfProductDetailRepository(db);
        var detail = await repo.FindAsync(ids.Active, false); Assert.NotNull(detail); Assert.Equal(10100L, detail.GiaBan);
        Assert.Equal(ids.Category, detail.DanhMuc.Id);
        var future = await repo.PricesAsync(ids.Active, new()); Assert.Equal(99900L, Assert.Single(future.Items).GiaBan);
        var first = await repo.PricesAsync(ids.Active, new() { DaLenLich = false, Size = 1 });
        Assert.Equal(4, first.Total); Assert.Equal(88800L, Assert.Single(first.Items).GiaBan); Assert.NotNull(first.Items[0].HuyLuc);
        var second = await repo.PricesAsync(ids.Active, new() { DaLenLich = false, Page = 2, Size = 1 });
        Assert.Equal(99900L, Assert.Single(second.Items).GiaBan); Assert.NotEqual(first.Items[0].Id, second.Items[0].Id);
        Assert.Null(await repo.FindAsync(ids.Inactive, true)); Assert.Null(await repo.FindAsync(4294967295L, false));
        Assert.Empty(db.ChangeTracker.Entries());
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repo.PricesAsync(ids.Active, new(), cancel.Token));
    }
}
