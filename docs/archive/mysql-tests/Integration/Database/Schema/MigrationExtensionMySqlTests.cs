using ConvenienceStore.Web.Tests.HaTang;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.Data.MySqlClient;

namespace ConvenienceStore.Web.Tests.Integration.Database.Schema;

public sealed class MigrationExtensionMySqlTests(CsdlMySqlTam database) : IClassFixture<CsdlMySqlTam>
{
    private async Task<long> Count(string sql) => Convert.ToInt64(await database.DocMotGiaTriAsync(sql));

    [Fact]
    public async Task LatestSchemaIncludesAllThreeExtensionTablesAndConstraints()
    {
        Assert.Equal(31, await Count("SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_TYPE='BASE TABLE' AND TABLE_NAME<>'__EFMigrationsHistory'"));
        Assert.Equal(3, await Count("SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME IN ('hoan_tien_hoa_don','quy_tien_mat','giao_dich_tien_mat')"));
        Assert.Equal(5, await Count("SELECT COUNT(*) FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_TYPE='FOREIGN KEY' AND TABLE_NAME IN ('hoan_tien_hoa_don','giao_dich_tien_mat')"));
        Assert.Equal(3, await Count("SELECT COUNT(*) FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_TYPE='CHECK' AND TABLE_NAME IN ('quy_tien_mat','giao_dich_tien_mat')"));
        Assert.Equal(2, await Count("SELECT COUNT(*) FROM information_schema.TRIGGERS WHERE TRIGGER_SCHEMA=DATABASE() AND EVENT_OBJECT_TABLE='hoan_tien_hoa_don'"));
        Assert.Equal(2, await Count("SELECT COUNT(*) FROM information_schema.TRIGGERS WHERE TRIGGER_SCHEMA=DATABASE() AND EVENT_OBJECT_TABLE='giao_dich_tien_mat'"));
        Assert.Equal(2, await Count("SELECT COUNT(DISTINCT INDEX_NAME) FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='giao_dich_tien_mat' AND INDEX_NAME IN ('uq_gdtm_loai_hoa_don','uq_gdtm_loai_phieu') AND NON_UNIQUE=0"));
        Assert.Equal(1, await Count("SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='chuong_trinh_khuyen_mai' AND COLUMN_NAME='anh_url' AND IS_NULLABLE='YES' AND CHARACTER_MAXIMUM_LENGTH=2048"));
    }

    [Fact]
    public async Task ExtensionsCanBeRolledBackAndReappliedOnDisposableDatabase()
    {
        await using var context = database.TaoDbContext();
        // Keep rollback/reapply on one physical connection; pooled connections can
        // retain the provider's server-wide advisory migration lock between calls.
        context.Database.SetConnectionString(new MySqlConnectionStringBuilder(database.ChuoiKetNoi) {
            Pooling = false
        }.ConnectionString);
        await context.Database.OpenConnectionAsync();
        var migrator = context.GetService<IMigrator>();
        try
        {
            await migrator.MigrateAsync("20261026120030_InitialCreate");
            Assert.Equal(0, await Count("SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME IN ('hoan_tien_hoa_don','quy_tien_mat','giao_dich_tien_mat')"));
            Assert.Equal(28, await Count("SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_TYPE='BASE TABLE' AND TABLE_NAME<>'__EFMigrationsHistory'"));
        }
        finally
        {
            await migrator.MigrateAsync();
        }
        Assert.Equal(context.Database.GetMigrations().Count(), (await context.Database.GetAppliedMigrationsAsync()).Count());
        Assert.Equal(0, await Count("SELECT so_du FROM quy_tien_mat WHERE id=1"));
    }

    [Fact]
    public async Task CashReconciliationStopsBeforeSchemaChangesWhenEventsAreDuplicatedAndCanBeRerun()
    {
        await using var context = database.TaoDbContext();
        context.Database.SetConnectionString(new MySqlConnectionStringBuilder(database.ChuoiKetNoi) {
            Pooling = false
        }.ConnectionString);
        await context.Database.OpenConnectionAsync();
        var migrator = context.GetService<IMigrator>();
        try
        {
            await migrator.MigrateAsync("20261112170000_CashLedger");
            await database.ChayAsync("""
                INSERT INTO tai_khoan(ten_dang_nhap,mat_khau_bam,ho_ten,vai_tro_id,bat_buoc_doi_mk)
                SELECT 'cash_guard_actor','unused','Cash guard test',id,0 FROM vai_tro WHERE ma='TN';
                INSERT INTO gio_hang(thu_ngan_id) SELECT id FROM tai_khoan WHERE ten_dang_nhap='cash_guard_actor';
                INSERT INTO hoa_don(ma_hoa_don,gio_hang_id,thu_ngan_id,thanh_toan_luc,tong_tien_hang,tong_thanh_toan,tien_khach_dua,tien_thua)
                SELECT 'HD-CASH-GUARD',g.id,g.thu_ngan_id,NOW(),10000,10000,10000,0
                FROM gio_hang g JOIN tai_khoan a ON a.id=g.thu_ngan_id WHERE a.ten_dang_nhap='cash_guard_actor';
                INSERT INTO giao_dich_tien_mat(loai,hoa_don_id,so_tien,so_du_truoc,so_du_sau,nguoi_thuc_hien_id)
                SELECT 'THU_BAN_HANG',h.id,10000,0,10000,h.thu_ngan_id FROM hoa_don h WHERE h.ma_hoa_don='HD-CASH-GUARD';
                INSERT INTO giao_dich_tien_mat(loai,hoa_don_id,so_tien,so_du_truoc,so_du_sau,nguoi_thuc_hien_id)
                SELECT 'THU_BAN_HANG',h.id,10000,10000,20000,h.thu_ngan_id FROM hoa_don h WHERE h.ma_hoa_don='HD-CASH-GUARD';
                """);

            // The duplicate check runs before any DDL, so a failed upgrade leaves the schema unchanged.
            await Assert.ThrowsAsync<MySqlException>(() => migrator.MigrateAsync("20261123090000_CashLedgerReconciliation"));
            Assert.Equal(0, await Count("SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='quy_tien_mat' AND COLUMN_NAME='da_chot_so_du_dau'"));
            Assert.DoesNotContain("20261123090000_CashLedgerReconciliation", await context.Database.GetAppliedMigrationsAsync());

            // After the duplicate is investigated and removed, the same migration runs cleanly.
            await database.ChayAsync("""
                DELETE g FROM giao_dich_tien_mat g JOIN hoa_don h ON h.id=g.hoa_don_id
                WHERE h.ma_hoa_don='HD-CASH-GUARD' AND g.so_du_truoc=10000;
                """);
            await migrator.MigrateAsync("20261123090000_CashLedgerReconciliation");
            Assert.Equal(1, await Count("SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='quy_tien_mat' AND COLUMN_NAME='da_chot_so_du_dau'"));
        }
        finally
        {
            await migrator.MigrateAsync();
        }
    }

    [Fact]
    public async Task CancellationRefundAuditRejectsUpdatesAndDeletes()
    {
        await database.ChayAsync("""
            INSERT INTO tai_khoan(ten_dang_nhap,mat_khau_bam,ho_ten,vai_tro_id,bat_buoc_doi_mk)
            SELECT 'migration_actor','unused','Migration test',id,0 FROM vai_tro WHERE ma='TN';
            INSERT INTO gio_hang(thu_ngan_id) SELECT id FROM tai_khoan WHERE ten_dang_nhap='migration_actor';
            INSERT INTO hoa_don(ma_hoa_don,gio_hang_id,thu_ngan_id,thanh_toan_luc,tong_tien_hang,tong_thanh_toan,tien_khach_dua,tien_thua)
            SELECT 'HD-MIGRATION-TEST',g.id,g.thu_ngan_id,NOW(),10000,10000,10000,0
            FROM gio_hang g JOIN tai_khoan a ON a.id=g.thu_ngan_id WHERE a.ten_dang_nhap='migration_actor';
            INSERT INTO hoan_tien_hoa_don(hoa_don_id,so_tien_hoan,ly_do,nguoi_thuc_hien_id)
            SELECT id,10000,'Migration regression',thu_ngan_id FROM hoa_don WHERE ma_hoa_don='HD-MIGRATION-TEST';
            """);
        var update = await Assert.ThrowsAsync<MySqlException>(() => database.ChayAsync("UPDATE hoan_tien_hoa_don SET ly_do='changed'"));
        var delete = await Assert.ThrowsAsync<MySqlException>(() => database.ChayAsync("DELETE FROM hoan_tien_hoa_don"));
        Assert.Equal(1644, update.Number);
        Assert.Equal(1644, delete.Number);
    }
}
