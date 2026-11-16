using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Modules.Products.Services;
using ConvenienceStore.Web.Services.Pos;
using ConvenienceStore.Web.Shared.Errors;
using ConvenienceStore.Web.Tests.HaTang;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;

namespace ConvenienceStore.Web.Tests.Modules.Products;

public sealed class Sp06LifecycleMySqlTests(CsdlMySqlTam database) : IClassFixture<CsdlMySqlTam>
{
    [Theory]
    [InlineData("invoice")]
    [InlineData("receipt")]
    [InlineData("cart")]
    [InlineData("ledger")]
    [InlineData("count")]
    [InlineData("disposal")]
    [InlineData("promotion")]
    public async Task EveryExternalReferencePreventsPhysicalDeletion(string kind)
    {
        var ids = await ProductReferenceSeed.Unused(database); var key = "REF-" + Guid.NewGuid().ToString("N")[..12];
        switch(kind) {
            case "invoice": await ProductReferenceSeed.Invoice(database, ids.Product); break;
            case "receipt":
            case "cart": await ProductReferenceSeed.HeldAndDraft(database, ids.Product, ids.Actor); break;
            case "ledger": await database.ChayAsync("""
                INSERT INTO the_kho(san_pham_id,loai_bien_dong,ma_chung_tu,chung_tu_id,so_luong_thay_doi,ton_sau,gia_von_ap_dung,gia_von_sau,nguoi_thuc_hien_id)
                VALUES(@id,'BAN_HANG',@key,1,-1,0,8000,8000,@actor)
                """, ("@id", ids.Product), ("@key", key), ("@actor", ids.Actor)); break;
            case "count": await database.ChayAsync("""
                INSERT INTO phieu_kiem_ke(ma_phieu,pham_vi,nguoi_lap_id) VALUES(@key,'CHON_SAN_PHAM',@actor);
                INSERT INTO ct_kiem_ke(phieu_kiem_ke_id,san_pham_id,ton_luc_lap) SELECT id,@id,0 FROM phieu_kiem_ke WHERE ma_phieu=@key
                """, ("@key", key), ("@id", ids.Product), ("@actor", ids.Actor)); break;
            case "disposal": await database.ChayAsync("""
                INSERT INTO phieu_xuat_huy(ma_phieu,nguoi_lap_id) VALUES(@key,@actor);
                INSERT INTO ct_xuat_huy(phieu_xuat_huy_id,san_pham_id,so_luong,ly_do) SELECT id,@id,1,'HONG' FROM phieu_xuat_huy WHERE ma_phieu=@key
                """, ("@key", key), ("@id", ids.Product), ("@actor", ids.Actor)); break;
            case "promotion": await database.ChayAsync("""
                INSERT INTO chuong_trinh_khuyen_mai(ma,ten,loai_giam,gia_tri,bat_dau_luc,ket_thuc_luc,nguoi_tao_id)
                VALUES(@key,'Reference promotion','SO_TIEN',100,NOW(),NOW()+INTERVAL 1 DAY,@actor);
                INSERT INTO khuyen_mai_san_pham(chuong_trinh_id,san_pham_id) SELECT id,@id FROM chuong_trinh_khuyen_mai WHERE ma=@key
                """, ("@key", key), ("@id", ids.Product), ("@actor", ids.Actor)); break;
        }
        await using var db = database.TaoDbContext(); var repo = new EfProductLifecycleRepository(db);
        Assert.True((await repo.PreviewAsync(ids.Product))!.CoThamChieu);
        Assert.Equal(409, (await Assert.ThrowsAsync<LoiNghiepVu>(() => repo.ExecuteAsync(ids.Product, ProductLifecycleOperation.Delete, new() { PhienBan = 1, XacNhan = true }))).MaHttp);
        Assert.Equal(1, Convert.ToInt32(await database.DocMotGiaTriAsync("SELECT COUNT(*) FROM san_pham WHERE id=@id", ("@id", ids.Product))));
        Assert.Equal(1, Convert.ToInt32(await database.DocMotGiaTriAsync("SELECT COUNT(*) FROM lich_su_gia WHERE san_pham_id=@id", ("@id", ids.Product))));
    }

    [Fact]
    public async Task DiscontinueAndReactivateKeepInvoiceSnapshotAndPosFilterCompatible()
    {
        var ids = await ProductReferenceSeed.Unused(database); var invoice = await ProductReferenceSeed.Invoice(database, ids.Product);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["ConnectionStrings:CuaHang"] = database.ChuoiKetNoi }).Build(); var catalog = new MySqlPosCatalog(config);
        Assert.NotNull(await catalog.FindAsync((int)ids.Product));
        await using var db = database.TaoDbContext(); var repo = new EfProductLifecycleRepository(db);
        await repo.ExecuteAsync(ids.Product, ProductLifecycleOperation.Discontinue, new() { PhienBan = 1, XacNhan = true }); Assert.Null(await catalog.FindAsync((int)ids.Product));
        await repo.ExecuteAsync(ids.Product, ProductLifecycleOperation.Reactivate, new() { PhienBan = 2, XacNhan = true }); Assert.NotNull(await catalog.FindAsync((int)ids.Product));
        Assert.Equal("Original snapshot", Convert.ToString(await database.DocMotGiaTriAsync("SELECT ten_san_pham FROM ct_hoa_don WHERE hoa_don_id=@invoice AND san_pham_id=@id", ("@invoice", invoice), ("@id", ids.Product))));
        Assert.Equal(10000, Convert.ToInt32(await database.DocMotGiaTriAsync("SELECT don_gia FROM ct_hoa_don WHERE hoa_don_id=@invoice AND san_pham_id=@id", ("@invoice", invoice), ("@id", ids.Product))));
        Assert.Equal(0, Convert.ToInt32(await database.DocMotGiaTriAsync("SELECT so_luong_ton FROM ton_kho WHERE san_pham_id=@id", ("@id", ids.Product))));
    }

    [Fact]
    public async Task DeleteFailureRestoresProductPriceAndStockRows()
    {
        var ids = await ProductReferenceSeed.Unused(database);
        await database.ChayAsync("CREATE TRIGGER sp06_reject_delete BEFORE DELETE ON san_pham FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Injected delete failure'");
        try {
            await using var db = database.TaoDbContext(); var repo = new EfProductLifecycleRepository(db);
            await Assert.ThrowsAsync<MySqlException>(() => repo.ExecuteAsync(ids.Product, ProductLifecycleOperation.Delete, new() { PhienBan = 1, XacNhan = true }));
        }
        finally { await database.ChayAsync("DROP TRIGGER sp06_reject_delete"); }
        foreach(var table in new[] { "lich_su_gia", "ton_kho" }) Assert.Equal(1, Convert.ToInt32(await database.DocMotGiaTriAsync($"SELECT COUNT(*) FROM {table} WHERE san_pham_id=@id", ("@id", ids.Product))));
        Assert.Equal(1, Convert.ToInt32(await database.DocMotGiaTriAsync("SELECT phien_ban FROM san_pham WHERE id=@id", ("@id", ids.Product))));
        await using var context = database.TaoDbContext(); using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new EfProductLifecycleRepository(context).ExecuteAsync(ids.Product, ProductLifecycleOperation.Delete, new() { PhienBan = 1, XacNhan = true }, cancellation.Token));
    }

    [Fact]
    public async Task ConcurrentReferenceCommitCannotCreateDanglingProduct()
    {
        var ids = await ProductReferenceSeed.Unused(database); var key = "race_" + Guid.NewGuid().ToString("N");
        await database.ChayAsync("INSERT INTO tai_khoan(ten_dang_nhap,mat_khau_bam,ho_ten,vai_tro_id) SELECT @key,'unused','Race cashier',id FROM vai_tro WHERE ma='TN'; INSERT INTO gio_hang(thu_ngan_id) SELECT id FROM tai_khoan WHERE ten_dang_nhap=@key", ("@key", key));
        var cart = Convert.ToUInt32(await database.DocMotGiaTriAsync("SELECT g.id FROM gio_hang g JOIN tai_khoan t ON t.id=g.thu_ngan_id WHERE t.ten_dang_nhap=@key", ("@key", key)));
        await using var connection = await database.MoKetNoiAsync(); await using var transaction = await connection.BeginTransactionAsync();
        await using(var command = CsdlMySqlTam.TaoLenh(connection, "INSERT INTO ct_gio_hang(gio_hang_id,san_pham_id,so_luong,don_gia) VALUES(@cart,@product,1,10000)", ("@cart", cart), ("@product", ids.Product))) { command.Transaction = (MySqlTransaction)transaction; await command.ExecuteNonQueryAsync(); }
        await using var db = database.TaoDbContext(); var deletion = new EfProductLifecycleRepository(db).ExecuteAsync(ids.Product, ProductLifecycleOperation.Delete, new() { PhienBan = 1, XacNhan = true });
        await transaction.CommitAsync(); Assert.Equal(409, (await Assert.ThrowsAsync<LoiNghiepVu>(() => deletion)).MaHttp);
        Assert.Equal(1, Convert.ToInt32(await database.DocMotGiaTriAsync("SELECT COUNT(*) FROM ct_gio_hang WHERE gio_hang_id=@cart AND san_pham_id=@id", ("@cart", cart), ("@id", ids.Product))));
        Assert.Equal(1, Convert.ToInt32(await database.DocMotGiaTriAsync("SELECT COUNT(*) FROM san_pham WHERE id=@id", ("@id", ids.Product))));
    }

    [Fact]
    public async Task ExternalProductForeignKeysNeverCascadeDeleteHistory()
    {
        Assert.Equal(0, Convert.ToInt32(await database.DocMotGiaTriAsync("SELECT COUNT(*) FROM information_schema.REFERENTIAL_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND REFERENCED_TABLE_NAME='san_pham' AND DELETE_RULE='CASCADE'")));
        Assert.Equal(7, Convert.ToInt32(await database.DocMotGiaTriAsync("SELECT COUNT(DISTINCT TABLE_NAME) FROM information_schema.REFERENTIAL_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND REFERENCED_TABLE_NAME='san_pham' AND TABLE_NAME IN('the_kho','ct_hoa_don','ct_phieu_nhap','ct_kiem_ke','ct_xuat_huy','ct_gio_hang','khuyen_mai_san_pham')")));
    }
}
