using ConvenienceStore.Web.Modules.Products.Services;
using ConvenienceStore.Web.Services.Pos;
using ConvenienceStore.Web.Tests.HaTang;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using ConvenienceStore.Web.Shared.Errors;

namespace ConvenienceStore.Web.Tests.Modules.Products;

public sealed class Sp05PricingMySqlTests(CsdlMySqlTam database) : IClassFixture<CsdlMySqlTam>
{
    [Fact]
    public async Task DatabasePriceCheckCancellationPairAndForeignKeysAreEnforced()
    {
        var ids = await ProductReadSeed.Create(database); var actor = Convert.ToUInt32(await database.DocMotGiaTriAsync("SELECT nguoi_tao_id FROM san_pham WHERE id=@id", ("@id", ids.Active)));
        var badPrice = await Assert.ThrowsAsync<MySqlException>(() => database.ChayAsync("INSERT INTO lich_su_gia(san_pham_id,gia_ban,hieu_luc_tu,nguoi_tao_id) VALUES(@id,101,NOW(),@actor)", ("@id", ids.Active), ("@actor", actor)));
        Assert.Contains("ck_lsg_gia", badPrice.Message);
        var badPair = await Assert.ThrowsAsync<MySqlException>(() => database.ChayAsync("INSERT INTO lich_su_gia(san_pham_id,gia_ban,hieu_luc_tu,nguoi_tao_id,huy_luc) VALUES(@id,10000,NOW(),@actor,NOW())", ("@id", ids.Active), ("@actor", actor)));
        Assert.Contains("ck_lsg_huy", badPair.Message);
        var badProduct = await Assert.ThrowsAsync<MySqlException>(() => database.ChayAsync("INSERT INTO lich_su_gia(san_pham_id,gia_ban,hieu_luc_tu,nguoi_tao_id) VALUES(4294967295,10000,NOW(),@actor)", ("@actor", actor)));
        Assert.Contains("fk_lsg_san_pham", badProduct.Message);
        Assert.Equal(2, Convert.ToInt32(await database.DocMotGiaTriAsync("SELECT COUNT(*) FROM lich_su_gia WHERE san_pham_id=@id", ("@id", ids.Active))));
    }

    [Fact]
    public async Task PosAndProductsResolveCurrentPriceAtTheSameVietnamTime()
    {
        var ids = await ProductReadSeed.Create(database);
        await database.ChayAsync("INSERT INTO lich_su_gia(san_pham_id,gia_ban,hieu_luc_tu,nguoi_tao_id) SELECT id,10100,NOW(),nguoi_tao_id FROM san_pham WHERE id=@id", ("@id", ids.Active));
        await using var db = database.TaoDbContext(); Assert.Equal(10100L, (await new EfProductDetailRepository(db).FindAsync(ids.Active, false))!.GiaBan);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["ConnectionStrings:CuaHang"] = database.ChuoiKetNoi }).Build();
        Assert.Equal(10100L, (await new MySqlPosCatalog(config).FindAsync((int)ids.Active))!.Price);
    }

    [Fact]
    public async Task PriceHistoryAndActiveCartSnapshotRemainConsistentAtEffectiveBoundary()
    {
        var ids = await ProductReadSeed.Create(database); var key = "cart_" + Guid.NewGuid().ToString("N");
        await database.ChayAsync("""
            INSERT INTO tai_khoan(ten_dang_nhap,mat_khau_bam,ho_ten,vai_tro_id) SELECT @key,'unused','Price cashier',id FROM vai_tro WHERE ma='TN';
            INSERT INTO gio_hang(thu_ngan_id) SELECT id FROM tai_khoan WHERE ten_dang_nhap=@key;
            INSERT INTO ct_gio_hang(gio_hang_id,san_pham_id,so_luong,don_gia)
            SELECT g.id,@id,1,10000 FROM gio_hang g JOIN tai_khoan t ON t.id=g.thu_ngan_id WHERE t.ten_dang_nhap=@key;
            """, ("@key", key), ("@id", ids.Active));
        var actor = Convert.ToUInt32(await database.DocMotGiaTriAsync("SELECT nguoi_tao_id FROM san_pham WHERE id=@id", ("@id", ids.Active)));
        await using var db = database.TaoDbContext(); var repo = new EfProductPricingRepository(db);
        var immediate = await repo.CreateAsync(ids.Active, new() { GiaBan = 12000, PhienBan = 1 }, actor);
        Assert.Equal(2L, immediate.PhienBan); Assert.Equal(actor, immediate.NguoiTaoId); Assert.True(immediate.NgayTao.Year >= 2026);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["ConnectionStrings:CuaHang"] = database.ChuoiKetNoi }).Build(); var catalog = new MySqlPosCatalog(config);
        Assert.Equal(12000L, (await catalog.FindAsync((int)ids.Active))!.Price); Assert.Equal(12000L, Assert.Single(await catalog.SearchAsync(null, (int)ids.Category)).Price);
        Assert.Equal(10000, Convert.ToInt32(await database.DocMotGiaTriAsync("SELECT don_gia FROM ct_gio_hang WHERE san_pham_id=@id", ("@id", ids.Active))));
        Assert.Equal(409, (await Assert.ThrowsAsync<LoiNghiepVu>(() => repo.CancelAsync(ids.Active, immediate.Id, 2, actor))).MaHttp);
        var future = await repo.CreateAsync(ids.Active, new() { GiaBan = 13000, PhienBan = 2, HieuLucTu = DateTimeOffset.UtcNow.AddDays(2).ToString("O") }, actor);
        // Controlled fixture data reaches the exact NOW boundary without sleeping or a scheduler.
        await database.ChayAsync("UPDATE lich_su_gia SET hieu_luc_tu=NOW() WHERE id=@id", ("@id", future.Id));
        Assert.Equal(409, (await Assert.ThrowsAsync<LoiNghiepVu>(() => repo.CancelAsync(ids.Active, future.Id, 3, actor))).MaHttp);
        Assert.Null((await repo.FindAsync(ids.Active, future.Id))!.HuyLuc); Assert.Equal(13000L, (await catalog.FindAsync((int)ids.Active))!.Price);
        Assert.Equal(10000, Convert.ToInt32(await database.DocMotGiaTriAsync("SELECT don_gia FROM ct_gio_hang WHERE san_pham_id=@id", ("@id", ids.Active))));
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repo.CreateAsync(ids.Active, new() { GiaBan = 14000, PhienBan = 3 }, actor, cancel.Token));
    }

    [Fact]
    public async Task HistoryInsertFailureRollsBackProductVersion()
    {
        var ids = await ProductReadSeed.Create(database); var actor = Convert.ToUInt32(await database.DocMotGiaTriAsync("SELECT nguoi_tao_id FROM san_pham WHERE id=@id", ("@id", ids.Active)));
        await database.ChayAsync("CREATE TRIGGER sp05_reject_price BEFORE INSERT ON lich_su_gia FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Injected history failure'");
        try {
            await using var db = database.TaoDbContext();
            await Assert.ThrowsAsync<DbUpdateException>(() => new EfProductPricingRepository(db).CreateAsync(ids.Active, new() { GiaBan = 12000, PhienBan = 1 }, actor));
        }
        finally { await database.ChayAsync("DROP TRIGGER sp05_reject_price"); }
        Assert.Equal(1, Convert.ToInt32(await database.DocMotGiaTriAsync("SELECT phien_ban FROM san_pham WHERE id=@id", ("@id", ids.Active))));
        Assert.Equal(2, Convert.ToInt32(await database.DocMotGiaTriAsync("SELECT COUNT(*) FROM lich_su_gia WHERE san_pham_id=@id", ("@id", ids.Active))));
    }

    [Fact]
    public async Task VersionWriteFailureRollsBackCancellationMetadata()
    {
        var ids = await ProductReadSeed.Create(database); var actor = Convert.ToUInt32(await database.DocMotGiaTriAsync("SELECT nguoi_tao_id FROM san_pham WHERE id=@id", ("@id", ids.Active)));
        await using var db = database.TaoDbContext(); var repo = new EfProductPricingRepository(db);
        var price = await repo.CreateAsync(ids.Active, new() { GiaBan = 12000, PhienBan = 1, HieuLucTu = DateTimeOffset.UtcNow.AddDays(2).ToString("O") }, actor);
        await database.ChayAsync("CREATE TRIGGER sp05_reject_version BEFORE UPDATE ON san_pham FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Injected version failure'");
        try { await Assert.ThrowsAsync<MySqlException>(() => repo.CancelAsync(ids.Active, price.Id, 2, actor)); }
        finally { await database.ChayAsync("DROP TRIGGER sp05_reject_version"); }
        var unchanged = await repo.FindAsync(ids.Active, price.Id); Assert.NotNull(unchanged); Assert.Null(unchanged.HuyLuc); Assert.Null(unchanged.NguoiHuyId); Assert.Equal(2L, unchanged.PhienBan);
    }
}
