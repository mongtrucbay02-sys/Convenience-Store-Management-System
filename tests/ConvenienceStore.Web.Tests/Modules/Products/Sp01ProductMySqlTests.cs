using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Modules.Products.Services;
using ConvenienceStore.Web.Tests.HaTang;

namespace ConvenienceStore.Web.Tests.Modules.Products;

public sealed class Sp01ProductMySqlTests(CsdlMySqlTam database) : IClassFixture<CsdlMySqlTam>
{
    [Fact]
    public async Task AccentsStablePagesCurrentPriceAndCancellationTranslateOnMySql()
    {
        var ids = await ProductReadSeed.Create(database);
        await using var db = database.TaoDbContext(); var repo = new EfProductReadRepository(db);
        var query = new ProductListQuery { DanhMucId = ids.Category, Keyword = "sua", Size = 1, Sort = "-giaBan,ma" };
        var first = await repo.ListAsync(query, false); var second = await repo.ListAsync(query with { Page = 2 }, false);
        Assert.Equal(2, first.Total); Assert.Equal(ids.Inactive, Assert.Single(first.Items).Id);
        Assert.Equal(ids.Active, Assert.Single(second.Items).Id); Assert.Equal(10000L, second.Items[0].GiaBan);
        Assert.Empty((await repo.ListAsync(query with { Page = 3 }, false)).Items);
        Assert.Empty((await repo.ListAsync(query with { TrangThai = "NGUNG_KINH_DOANH" }, true)).Items);
        Assert.Empty((await repo.ListAsync(query with { Keyword = "%" }, false)).Items);
        Assert.Empty((await repo.ListAsync(query with { Keyword = "_" }, false)).Items);
        var code = second.Items[0].Ma;
        Assert.Single((await repo.ListAsync(query with { Keyword = code }, false)).Items);
        Assert.Empty(db.ChangeTracker.Entries());
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repo.ListAsync(query, false, cancel.Token));
    }
}
