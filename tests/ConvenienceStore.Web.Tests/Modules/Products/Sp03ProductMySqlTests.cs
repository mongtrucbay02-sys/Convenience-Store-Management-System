using ConvenienceStore.Web.Modules.Inventory.Models;
using ConvenienceStore.Web.Modules.Inventory.Services;
using ConvenienceStore.Web.Modules.Products.Services;
using ConvenienceStore.Web.Shared.Errors;
using ConvenienceStore.Web.Tests.HaTang;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace ConvenienceStore.Web.Tests.Modules.Products;

public sealed class Sp03ProductMySqlTests(CsdlMySqlTam database) : IClassFixture<CsdlMySqlTam>
{
    [Fact]
    public async Task CreationPersistsAllThreeRecordsAndCurrentPrice()
    {
        var ids = await ProductReadSeed.Create(database);
        var actor = Convert.ToUInt32(await database.DocMotGiaTriAsync("SELECT nguoi_tao_id FROM san_pham WHERE id=@id", ("@id", ids.Active)));
        await using var db = database.TaoDbContext();
        var repo = new EfProductCreationRepository(db, new DichVuKho(db, NullLogger<DichVuKho>.Instance));
        var input = Sp03ProductUnitTests.Valid() with { Ma = "MYSQL-" + Guid.NewGuid().ToString("N")[..12], DanhMucId = ids.Category };
        var id = await repo.CreateAsync(input, actor);
        var stock = await db.TonKhos.AsNoTracking().SingleAsync(x => x.SanPhamId == id); Assert.Equal(0, stock.SoLuongTon); Assert.Equal(0, stock.GiaVonBinhQuan); Assert.Equal(5, stock.NguongToiThieu);
        Assert.Equal(1, await db.LichSuGia.CountAsync(x => x.SanPhamId == id));
        Assert.Equal(10000L, (await db.VGiaBanHienHanhs.SingleAsync(x => x.SanPhamId == id)).GiaBan);
        Assert.Equal(409, (await Assert.ThrowsAsync<LoiNghiepVu>(() => repo.CreateAsync(input, actor))).MaHttp);
    }

    [Fact]
    public async Task InventoryFailureRollsBackProductAndInitialPrice()
    {
        var ids = await ProductReadSeed.Create(database);
        var actor = Convert.ToUInt32(await database.DocMotGiaTriAsync("SELECT nguoi_tao_id FROM san_pham WHERE id=@id", ("@id", ids.Active)));
        await using var db = database.TaoDbContext(); var inventory = new FailingStock(db); var repo = new EfProductCreationRepository(db, inventory);
        var code = "ROLLBACK-" + Guid.NewGuid().ToString("N")[..10];
        await Assert.ThrowsAsync<InvalidOperationException>(() => repo.CreateAsync(Sp03ProductUnitTests.Valid() with { Ma = code, DanhMucId = ids.Category }, actor));
        Assert.True(inventory.SawAllThreeRecordsInTransaction);
        Assert.Equal(0, Convert.ToInt32(await database.DocMotGiaTriAsync("SELECT COUNT(*) FROM san_pham WHERE ma=@code", ("@code", code))));
        Assert.Equal(0, Convert.ToInt32(await database.DocMotGiaTriAsync("SELECT COUNT(*) FROM lich_su_gia WHERE san_pham_id=@id", ("@id", inventory.Id))));
        Assert.Equal(0, Convert.ToInt32(await database.DocMotGiaTriAsync("SELECT COUNT(*) FROM ton_kho WHERE san_pham_id=@id", ("@id", inventory.Id))));
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repo.CreateAsync(Sp03ProductUnitTests.Valid() with { Ma = code, DanhMucId = ids.Category }, actor, ct: cancel.Token));
    }

    private sealed class FailingStock(ConvenienceStore.Web.Data.CuaHangDbContext db) : IDichVuKho
    {
        public bool SawAllThreeRecordsInTransaction; public int Id;
        public async Task KhoiTaoTonKhoAsync(int id, int threshold = 5, CancellationToken ct = default)
        {
            Id = id;
            await new DichVuKho(db, NullLogger<DichVuKho>.Instance).KhoiTaoTonKhoAsync(id, threshold, ct);
            SawAllThreeRecordsInTransaction = db.Database.CurrentTransaction is not null && await db.SanPhams.AnyAsync(x => x.Id == id, ct)
                && await db.LichSuGia.AnyAsync(x => x.SanPhamId == id, ct) && await db.TonKhos.AnyAsync(x => x.SanPhamId == id, ct);
            throw new InvalidOperationException("Injected inventory boundary failure after all three writes.");
        }
        public Task<KetQuaBienDong> GhiNhanBienDongAsync(YeuCauBienDong input, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<TonKhoHienTai>> LayTonKhoAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<int> DemSanPhamDuoiNguongAsync(CancellationToken ct = default) => throw new NotSupportedException();
    }
}
