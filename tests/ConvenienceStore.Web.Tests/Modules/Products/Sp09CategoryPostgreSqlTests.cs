using ConvenienceStore.Web.Data.Entities;
using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Modules.Products.Services;
using ConvenienceStore.Web.Services.Pos;
using ConvenienceStore.Web.Tests.HaTang;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace ConvenienceStore.Web.Tests.Modules.Products;

public sealed class Sp09CategoryPostgreSqlTests(CsdlPostgreSqlTam database) : IClassFixture<CsdlPostgreSqlTam>
{
    [Fact]
    public async Task HideDeleteNoOpAndTrackedRefreshReuseCentralCategoryVersion()
    {
        await using var db = database.TaoDbContext(); var first = new DanhMuc { Ten = "SP09-A-" + Guid.NewGuid().ToString("N"), KhongDoiTra = true }; var other = new DanhMuc { Ten = "SP09-B-" + Guid.NewGuid().ToString("N") };
        db.DanhMucs.AddRange(first, other); await db.SaveChangesAsync(); var repo = new EfCategoryRemovalRepository(db);
        Assert.True(db.Model.FindEntityType(typeof(DanhMuc))!.FindProperty(nameof(DanhMuc.PhienBan))!.IsConcurrencyToken);
        Assert.Equal(CategoryRemovalStatus.Success, (await repo.HideAsync(first.Id, 1)).Status); Assert.Equal(2L, first.PhienBan); Assert.Equal("DA_AN", first.TrangThai); Assert.True(first.KhongDoiTra);
        Assert.Equal(CategoryRemovalStatus.Conflict, (await repo.DeleteAsync(first.Id, 1)).Status);
        Assert.Equal(CategoryRemovalStatus.Success, (await repo.HideAsync(first.Id, 2)).Status); Assert.Equal(2L, first.PhienBan);
        Assert.Equal(CategoryRemovalStatus.Success, (await repo.DeleteAsync(first.Id, 2)).Status); Assert.False(await db.DanhMucs.AnyAsync(x => x.Id == first.Id)); Assert.True(await db.DanhMucs.AnyAsync(x => x.Id == other.Id));
        await database.ChayAsync("UPDATE danh_muc SET mo_ta='External edit',phien_ban=phien_ban+1 WHERE id=@id", ("@id", other.Id));
        Assert.Equal(CategoryRemovalStatus.Conflict, (await repo.HideAsync(other.Id, 1)).Status); Assert.Equal(2L, other.PhienBan); Assert.Equal("External edit", other.MoTa);
        Assert.Equal(CategoryRemovalStatus.Success, (await repo.HideAsync(other.Id, 2)).Status); Assert.Equal(3L, other.PhienBan);
        using var cancel = new CancellationTokenSource(); cancel.Cancel(); await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repo.HideAsync(other.Id, 3, cancel.Token));
    }

    [Fact]
    public async Task CategoryScopedCountDocumentPreventsDeletionEvenWhenNoProductsRemain()
    {
        var seed = await ProductReferenceSeed.Unused(database);
        await using var db = database.TaoDbContext(); var category = new DanhMuc { Ten = "SP09-COUNT-" + Guid.NewGuid().ToString("N") }; db.DanhMucs.Add(category); await db.SaveChangesAsync();
        await database.ChayAsync("INSERT INTO phieu_kiem_ke(ma_phieu,pham_vi,danh_muc_id,nguoi_lap_id) VALUES(@code,'DANH_MUC',@id,@actor)", ("@code", "C-" + Guid.NewGuid().ToString("N")[..15]), ("@id", category.Id), ("@actor", seed.Actor));
        var repo = new EfCategoryRemovalRepository(db); var preview = await repo.PreviewAsync(category.Id); Assert.Equal(0, preview!.SoSanPham); Assert.True(preview.CoPhieuKiemKe);
        Assert.Equal(CategoryRemovalStatus.Referenced, (await repo.DeleteAsync(category.Id, 1)).Status);
        Assert.Equal(CategoryRemovalStatus.Success, (await repo.HideAsync(category.Id, 1)).Status);
        Assert.Equal(1, Convert.ToInt32(await database.DocMotGiaTriAsync("SELECT COUNT(*) FROM phieu_kiem_ke WHERE danh_muc_id=@id", ("@id", category.Id))));
    }

    [Fact]
    public async Task ConcurrentHideHasOneVersionWinner()
    {
        await using var context = database.TaoDbContext(); var category = new DanhMuc { Ten = "SP09-RACE-" + Guid.NewGuid().ToString("N") }; context.DanhMucs.Add(category); await context.SaveChangesAsync();
        async Task<CategoryRemovalStatus> Hide() { await using var db = database.TaoDbContext(); return (await new EfCategoryRemovalRepository(db).HideAsync(category.Id, 1)).Status; }
        var outcomes = await Task.WhenAll(Hide(), Hide()); Assert.Single(outcomes, x => x == CategoryRemovalStatus.Success); Assert.Single(outcomes, x => x == CategoryRemovalStatus.Conflict);
        Assert.Equal(2, Convert.ToInt32(await database.DocMotGiaTriAsync("SELECT phien_ban FROM danh_muc WHERE id=@id", ("@id", category.Id))));
    }

    [Fact]
    public async Task ConcurrentProductReferenceCannotBecomeDangling()
    {
        var seed = await ProductReferenceSeed.Unused(database);
        await using var context = database.TaoDbContext(); var category = new DanhMuc { Ten = "SP09-FK-" + Guid.NewGuid().ToString("N") }; context.DanhMucs.Add(category); await context.SaveChangesAsync();
        await using var connection = await database.MoKetNoiAsync(); await using var transaction = await connection.BeginTransactionAsync();
        await using(var command = CsdlPostgreSqlTam.TaoLenh(connection, "INSERT INTO san_pham(ma,ten_san_pham,danh_muc_id,don_vi_tinh,nguoi_tao_id) VALUES(@code,'Concurrent product',@category,'Hộp',@actor)",
            ("@code", "FK-" + Guid.NewGuid().ToString("N")[..12]), ("@category", category.Id), ("@actor", seed.Actor))) { command.Transaction = (NpgsqlTransaction)transaction; await command.ExecuteNonQueryAsync(); }
        await using var db = database.TaoDbContext(); var deletion = new EfCategoryRemovalRepository(db).DeleteAsync(category.Id, 1);
        await transaction.CommitAsync(); Assert.Equal(CategoryRemovalStatus.Referenced, (await deletion).Status);
        Assert.Equal(1, Convert.ToInt32(await database.DocMotGiaTriAsync("SELECT COUNT(*) FROM danh_muc WHERE id=@id", ("@id", category.Id))));
        Assert.Equal(1, Convert.ToInt32(await database.DocMotGiaTriAsync("SELECT COUNT(*) FROM san_pham WHERE danh_muc_id=@id", ("@id", category.Id))));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task UnexpectedDatabaseErrorsRemainFailuresAndRollBack(bool hide)
    {
        long id;
        await using(var seed = database.TaoDbContext()) { var row = new DanhMuc { Ten = "SP09-ROLLBACK-" + Guid.NewGuid().ToString("N") }; seed.DanhMucs.Add(row); await seed.SaveChangesAsync(); id = row.Id; }
        var operation = hide ? "UPDATE" : "DELETE";
        await database.ChayAsync($"CREATE OR REPLACE FUNCTION sp09_reject_fn() RETURNS trigger LANGUAGE plpgsql AS $test$ BEGIN RAISE EXCEPTION 'Injected category failure'; END $test$; CREATE TRIGGER sp09_reject BEFORE {operation} ON danh_muc FOR EACH ROW EXECUTE FUNCTION sp09_reject_fn()");
        try { await using var db = database.TaoDbContext(); var repo = new EfCategoryRemovalRepository(db); await Assert.ThrowsAsync<DbUpdateException>(() => hide ? repo.HideAsync(id, 1) : repo.DeleteAsync(id, 1)); }
        finally { await database.ChayAsync("DROP TRIGGER sp09_reject ON danh_muc"); }
        Assert.Equal("HOAT_DONG", Convert.ToString(await database.DocMotGiaTriAsync("SELECT trang_thai FROM danh_muc WHERE id=@id", ("@id", id))));
        Assert.Equal(1, Convert.ToInt32(await database.DocMotGiaTriAsync("SELECT phien_ban FROM danh_muc WHERE id=@id", ("@id", id))));
    }

    [Fact]
    public async Task HidePreservesProductHistoryAndExistingPosCategoryFilters()
    {
        var ids = await ProductReadSeed.Create(database); var invoice = await ProductReferenceSeed.Invoice(database, ids.Active);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["ConnectionStrings:CuaHang"] = database.ChuoiKetNoi }).Build(); var pos = new PostgreSqlPosCatalog(config);
        Assert.NotNull(await pos.FindAsync((int)ids.Active));
        await using var db = database.TaoDbContext(); Assert.Equal(CategoryRemovalStatus.Success, (await new EfCategoryRemovalRepository(db).HideAsync(ids.Category, 1)).Status);
        Assert.Null(await pos.FindAsync((int)ids.Active)); Assert.Empty(await pos.SearchAsync(null, (int)ids.Category)); Assert.DoesNotContain(await pos.GetCategoriesAsync(), x => x.Id == ids.Category);
        Assert.Equal("Original snapshot", Convert.ToString(await database.DocMotGiaTriAsync("SELECT ten_san_pham FROM ct_hoa_don WHERE hoa_don_id=@id", ("@id", invoice))));
        Assert.Equal(2, Convert.ToInt32(await database.DocMotGiaTriAsync("SELECT COUNT(*) FROM san_pham WHERE danh_muc_id=@id", ("@id", ids.Category))));
        Assert.Equal("DANG_KINH_DOANH", Convert.ToString(await database.DocMotGiaTriAsync("SELECT trang_thai FROM san_pham WHERE id=@id", ("@id", ids.Active))));
    }
}
