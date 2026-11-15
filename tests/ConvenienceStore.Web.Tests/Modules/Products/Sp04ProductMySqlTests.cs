using ConvenienceStore.Web.Modules.Products.Services;
using ConvenienceStore.Web.Shared.Errors;
using ConvenienceStore.Web.Tests.HaTang;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

namespace ConvenienceStore.Web.Tests.Modules.Products;

public sealed class Sp04ProductMySqlTests(CsdlMySqlTam database) : IClassFixture<CsdlMySqlTam>
{
    [Fact]
    public async Task ParallelMetadataWritesCommitExactlyOnce()
    {
        var ids = await ProductReadSeed.Create(database);
        async Task<int> Write(string name) {
            await using var db = database.TaoDbContext(); var repo = new EfProductEditRepository(db);
            try { await repo.UpdateAsync(ids.Active, Sp04ProductUnitTests.Valid() with { TenSanPham = name, DanhMucId = ids.Category }); return 200; }
            catch (LoiNghiepVu ex) { return ex.MaHttp; }
        }
        var outcomes = await Task.WhenAll(Write("Writer A"), Write("Writer B")); Assert.Equal(new[] { 200, 409 }, outcomes.Order().ToArray());
        Assert.Equal(2, Convert.ToInt32(await database.DocMotGiaTriAsync("SELECT phien_ban FROM san_pham WHERE id=@id", ("@id", ids.Active))));
    }

    [Fact]
    public async Task InvoiceSnapshotIsPreservedAndUnitCannotChangeAfterStockReachesZero()
    {
        var ids = await ProductReadSeed.Create(database); var invoice = await ProductReferenceSeed.Invoice(database, ids.Active);
        await database.ChayAsync("UPDATE ton_kho SET so_luong_ton=0 WHERE san_pham_id=@id", ("@id", ids.Active));
        await using var db = database.TaoDbContext(); var repo = new EfProductEditRepository(db);
        var input = Sp04ProductUnitTests.Valid() with { TenSanPham = "Current renamed", DanhMucId = ids.Category };
        await repo.UpdateAsync(ids.Active, input);
        var snapshot = await db.CtHoaDons.AsNoTracking().SingleAsync(x => x.HoaDonId == invoice && x.SanPhamId == ids.Active);
        Assert.Equal("Original snapshot", snapshot.TenSanPham); Assert.Equal("Hộp", snapshot.DonViTinh); Assert.Equal(10000u, snapshot.DonGia); Assert.Equal(8000m, snapshot.GiaVon); Assert.Equal(10000ul, snapshot.ThanhTien);
        Assert.Equal(409, (await Assert.ThrowsAsync<LoiNghiepVu>(() => repo.UpdateAsync(ids.Active, input with { PhienBan = 2, DonViTinh = "Cái" }))).MaHttp);
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repo.UpdateAsync(ids.Active, input with { PhienBan = 2 }, ct: cancel.Token));
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [Fact]
    public async Task ThresholdFailureRollsBackMetadataVersionAndImageUrl()
    {
        var ids = await ProductReadSeed.Create(database);
        var original = Convert.ToString(await database.DocMotGiaTriAsync("SELECT ten_san_pham FROM san_pham WHERE id=@id", ("@id", ids.Active)));
        await database.ChayAsync("CREATE TRIGGER sp04_reject_threshold BEFORE UPDATE ON ton_kho FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Injected threshold write failure'");
        try {
            await using var db = database.TaoDbContext(); var repo = new EfProductEditRepository(db);
            await Assert.ThrowsAsync<MySqlException>(() => repo.UpdateAsync(ids.Active, Sp04ProductUnitTests.Valid() with { TenSanPham = "Must rollback", DanhMucId = ids.Category, NguongToiThieu = 9 }, "/uploads/products/candidate.png"));
        }
        finally { await database.ChayAsync("DROP TRIGGER sp04_reject_threshold"); }
        Assert.Equal(original, Convert.ToString(await database.DocMotGiaTriAsync("SELECT ten_san_pham FROM san_pham WHERE id=@id", ("@id", ids.Active))));
        Assert.Equal(1, Convert.ToInt32(await database.DocMotGiaTriAsync("SELECT phien_ban FROM san_pham WHERE id=@id", ("@id", ids.Active))));
        Assert.Null(await database.DocMotGiaTriAsync("SELECT anh_url FROM san_pham WHERE id=@id", ("@id", ids.Active)));
        Assert.Equal(5, Convert.ToInt32(await database.DocMotGiaTriAsync("SELECT nguong_toi_thieu FROM ton_kho WHERE san_pham_id=@id", ("@id", ids.Active))));
    }
}
