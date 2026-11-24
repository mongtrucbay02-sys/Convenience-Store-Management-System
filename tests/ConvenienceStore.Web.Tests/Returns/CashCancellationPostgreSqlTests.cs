using System.Security.Claims;
using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Returns.Services;
using ConvenienceStore.Web.Services.Pos;
using ConvenienceStore.Web.Shared.Data;
using ConvenienceStore.Web.Shared.Errors;
using ConvenienceStore.Web.Tests.Inventory;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ConvenienceStore.Web.Tests.Returns;

// A separate fixture prevents cash totals leaking into existing warehouse/returns tests.
public sealed class CashCancellationPostgreSqlTests(CsdlKhoFixture database) : IClassFixture<CsdlKhoFixture>
{
    private readonly CashLedgerService ledger = new();
    private ClaimsPrincipal Admin => new(new ClaimsIdentity([
        new Claim(ClaimTypes.NameIdentifier, database.TaiKhoanAdminId.ToString()), new Claim(ClaimTypes.Role, "AD")], "test"));
    private PosInvoiceCancellationService Service(CuaHangDbContext db) =>
        new(db, BoTroGoiDichVuKho.TaoDichVu(db), TimeProvider.System, ledger);
    private async Task<long> Scalar(string sql, params (string, object?)[] args) => Convert.ToInt64(await database.DocMotGiaTriAsync(sql, args));

    private async Task<(long Invoice, int Product, long Customer)> Seed(bool recordSale = true)
    {
        var product = await new BoTroGoiDichVuKho(database).TaoSanPhamCoTonAsync("CASH", 8, 10000);
        var customer = await Scalar("""
            INSERT INTO khach_hang(ho_ten,so_dien_thoai,diem_hien_co,nguoi_tao_id)
            VALUES ('Khách hủy',@phone,5,@actor) RETURNING id;
            """, ("@phone", "0" + Random.Shared.Next(100000000, 999999999)), ("@actor", database.TaiKhoanAdminId));
        var cart = await Scalar("INSERT INTO gio_hang(thu_ngan_id,trang_thai) VALUES (@actor,'DA_HOAN_TAT') RETURNING id;", ("@actor", database.TaiKhoanThuNganId));
        var invoice = await Scalar("""
            INSERT INTO hoa_don(ma_hoa_don,gio_hang_id,thu_ngan_id,khach_hang_id,thanh_toan_luc,
              tong_tien_hang,tong_thanh_toan,tien_khach_dua,tien_thua,diem_cong,so_tien_mot_diem)
            VALUES (@code,@cart,@actor,@customer,NOW(),50000,50000,100000,50000,5,10000) RETURNING id;
            """, ("@code", "HD" + Guid.NewGuid().ToString("N")[..12]), ("@cart", cart), ("@actor", database.TaiKhoanThuNganId), ("@customer", customer));
        await database.ChayAsync("""
            INSERT INTO ct_hoa_don(hoa_don_id,san_pham_id,ten_san_pham,don_vi_tinh,so_luong,don_gia,gia_von)
            VALUES (@invoice,@product,'Snapshot','Hộp',2,25000,10000)
            """, ("@invoice", invoice), ("@product", product));
        if (recordSale)
        {
            await using var db = database.TaoDbContext();
            await using var tx = await db.Database.BeginTransactionAsync();
            await ledger.RecordSaleAsync(db, invoice, database.TaiKhoanThuNganId, 50000, default);
            await tx.CommitAsync();
        }
        return (invoice, product, customer);
    }

    [Fact]
    public async Task CancelReconcilesCashStockPointsAndCannotRefundTwice()
    {
        var before = await Scalar("SELECT so_du FROM quy_tien_mat WHERE id=1");
        var seed = await Seed();
        await using (var db = database.TaoDbContext())
            Assert.Equal(50000L, (await Service(db).CancelAsync(seed.Invoice, "Khách đổi ý", Admin)).CashRefund);
        Assert.Equal(before, await Scalar("SELECT so_du FROM quy_tien_mat WHERE id=1"));
        Assert.Equal(10, await Scalar("SELECT so_luong_ton FROM ton_kho WHERE san_pham_id=@id", ("@id", seed.Product)));
        Assert.Equal(0, await Scalar("SELECT diem_hien_co FROM khach_hang WHERE id=@id", ("@id", seed.Customer)));
        Assert.Equal(1, await Scalar("SELECT COUNT(*) FROM giao_dich_tien_mat WHERE loai='CHI_HUY_HOA_DON' AND hoa_don_id=@id", ("@id", seed.Invoice)));
        await using var retry = database.TaoDbContext();
        Assert.Equal(409, (await Assert.ThrowsAsync<LoiNghiepVu>(() => Service(retry).CancelAsync(seed.Invoice, "Gửi lại", Admin))).MaHttp);
        Assert.Equal(before, await Scalar("SELECT so_du FROM quy_tien_mat WHERE id=1"));
    }

    [Fact]
    public async Task InsufficientFundRollsBackStockPointsAndInvoice()
    {
        var seed = await Seed(false);
        await database.ChayAsync("UPDATE quy_tien_mat SET so_du=0 WHERE id=1");
        await using var db = database.TaoDbContext();
        var error = await Assert.ThrowsAsync<LoiNghiepVu>(() => Service(db).CancelAsync(seed.Invoice, "Thiếu quỹ", Admin));
        Assert.Equal("QUY_KHONG_DU", error.Ma);
        Assert.Equal(8, await Scalar("SELECT so_luong_ton FROM ton_kho WHERE san_pham_id=@id", ("@id", seed.Product)));
        Assert.Equal(5, await Scalar("SELECT diem_hien_co FROM khach_hang WHERE id=@id", ("@id", seed.Customer)));
        Assert.Equal("DA_THANH_TOAN", await database.DocMotGiaTriAsync("SELECT trang_thai FROM hoa_don WHERE id=@id", ("@id", seed.Invoice)));
        Assert.Equal(0, await Scalar("SELECT COUNT(*) FROM hoan_tien_hoa_don WHERE hoa_don_id=@id", ("@id", seed.Invoice)));
    }

    [Fact]
    public async Task LaterFailureRollsBackRecordedCashAndOtherEffects()
    {
        var seed = await Seed();
        var before = await Scalar("SELECT so_du FROM quy_tien_mat WHERE id=1");
        await database.ChayAsync("""
            CREATE OR REPLACE FUNCTION test_fail_cancel_fn() RETURNS trigger LANGUAGE plpgsql AS $test$ BEGIN RAISE EXCEPTION 'injected audit failure'; END $test$; CREATE TRIGGER test_fail_cancel BEFORE INSERT ON hoan_tien_hoa_don FOR EACH ROW EXECUTE FUNCTION test_fail_cancel_fn()
            """);
        try
        {
            await using var db = database.TaoDbContext();
            await Assert.ThrowsAsync<PostgresException>(() => Service(db).CancelAsync(seed.Invoice, "Rollback", Admin));
        }
        finally { await database.ChayAsync("DROP TRIGGER test_fail_cancel ON hoan_tien_hoa_don"); }
        Assert.Equal(before, await Scalar("SELECT so_du FROM quy_tien_mat WHERE id=1"));
        Assert.Equal(0, await Scalar("SELECT COUNT(*) FROM giao_dich_tien_mat WHERE loai='CHI_HUY_HOA_DON' AND hoa_don_id=@id", ("@id", seed.Invoice)));
        Assert.Equal(8, await Scalar("SELECT so_luong_ton FROM ton_kho WHERE san_pham_id=@id", ("@id", seed.Product)));
        Assert.Equal(5, await Scalar("SELECT diem_hien_co FROM khach_hang WHERE id=@id", ("@id", seed.Customer)));
    }

    [Fact]
    public async Task ConcurrentCancellationHasOneWinner()
    {
        var seed = await Seed();
        async Task<bool> Cancel()
        {
            await using var db = database.TaoDbContext();
            try { await Service(db).CancelAsync(seed.Invoice, "Hai lần bấm", Admin); return true; }
            catch (LoiNghiepVu e) when (e.MaHttp == 409) { return false; }
        }
        Assert.Single(await Task.WhenAll(Cancel(), Cancel()), x => x);
        Assert.Equal(1, await Scalar("SELECT COUNT(*) FROM giao_dich_tien_mat WHERE loai='CHI_HUY_HOA_DON' AND hoa_don_id=@id", ("@id", seed.Invoice)));
    }

    [Fact]
    public async Task OpeningRequiresAdminRejectsStaleBalanceAndIsRecordedOnce()
    {
        await using var db = database.TaoDbContext();
        var before = await ledger.GetBalanceAsync(db, default);
        var denied = await Assert.ThrowsAsync<LoiNghiepVu>(() => ledger.ConfirmOpeningAsync(db, database.TaiKhoanThuNganId, 100000, before.Amount, "Count", default));
        Assert.Equal(403, denied.MaHttp);
        var stale = await Assert.ThrowsAsync<LoiNghiepVu>(() => ledger.ConfirmOpeningAsync(db, database.TaiKhoanAdminId, 100000, before.Amount + 1, "Count", default));
        Assert.Equal("QUY_DA_THAY_DOI", stale.Ma);
        await ledger.ConfirmOpeningAsync(db, database.TaiKhoanAdminId, 100000, before.Amount, "Đếm thực tế sau nâng cấp", default);
        Assert.Equal(new CashLedgerService.Balance(100000, true), await ledger.GetBalanceAsync(db, default));
        Assert.Equal("DA_CHOT_SO_DU_DAU", (await Assert.ThrowsAsync<LoiNghiepVu>(() => ledger.ConfirmOpeningAsync(db, database.TaiKhoanAdminId, 200000, 100000, "Again", default))).Ma);
        Assert.Equal(1, await Scalar("SELECT COUNT(*) FROM giao_dich_tien_mat WHERE loai='CHOT_SO_DU_DAU'"));
        Assert.Equal("23514", (await Assert.ThrowsAsync<PostgresException>(() => database.ChayAsync("UPDATE giao_dich_tien_mat SET so_tien=1 WHERE loai='CHOT_SO_DU_DAU'"))).SqlState);
        Assert.Equal("23514", (await Assert.ThrowsAsync<PostgresException>(() => database.ChayAsync("DELETE FROM giao_dich_tien_mat WHERE loai='CHOT_SO_DU_DAU'"))).SqlState);
    }

    [Fact]
    public async Task DatabaseRejectsDuplicateCashEvents()
    {
        var seed = await Seed();
        Assert.Equal("23505", (await Assert.ThrowsAsync<PostgresException>(() => database.ChayAsync("""
            INSERT INTO giao_dich_tien_mat(loai,hoa_don_id,so_tien,so_du_truoc,so_du_sau,nguoi_thuc_hien_id)
            SELECT loai,hoa_don_id,so_tien,so_du_truoc,so_du_sau,nguoi_thuc_hien_id
            FROM giao_dich_tien_mat WHERE hoa_don_id=@id AND loai='THU_BAN_HANG'
            """, ("@id", seed.Invoice)))).SqlState);
    }
}
