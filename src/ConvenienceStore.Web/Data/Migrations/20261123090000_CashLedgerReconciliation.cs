using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ConvenienceStore.Web.Data.Migrations;

[DbContext(typeof(CuaHangDbContext))]
[Migration("20261123090000_CashLedgerReconciliation")]
public sealed class CashLedgerReconciliation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // No historical cash is inferred or rewritten. An Admin must count and confirm it.
        migrationBuilder.Sql("ALTER TABLE quy_tien_mat ADD da_chot_so_du_dau BOOLEAN NOT NULL DEFAULT FALSE;");
        migrationBuilder.Sql("""
            ALTER TABLE giao_dich_tien_mat
              DROP CHECK ck_gdtm_so_tien,
              DROP CHECK ck_gdtm_lien_ket,
              MODIFY loai ENUM('THU_BAN_HANG','CHI_HOAN_TRA','CHI_HUY_HOA_DON','CHOT_SO_DU_DAU') NOT NULL,
              ADD ghi_chu VARCHAR(500) NULL,
              ADD CONSTRAINT ck_gdtm_so_tien CHECK (so_tien > 0 OR loai = 'CHOT_SO_DU_DAU'),
              ADD CONSTRAINT ck_gdtm_lien_ket CHECK (
                (loai IN ('THU_BAN_HANG','CHI_HUY_HOA_DON') AND hoa_don_id IS NOT NULL AND phieu_doi_tra_id IS NULL)
                OR (loai='CHI_HOAN_TRA' AND hoa_don_id IS NULL AND phieu_doi_tra_id IS NOT NULL)
                OR (loai='CHOT_SO_DU_DAU' AND hoa_don_id IS NULL AND phieu_doi_tra_id IS NULL AND ghi_chu IS NOT NULL)),
              ADD UNIQUE KEY uq_gdtm_loai_hoa_don (loai,hoa_don_id),
              ADD UNIQUE KEY uq_gdtm_loai_phieu (loai,phieu_doi_tra_id);
            """);
        // Existing duplicates cause the migration to fail for investigation, never silent deletion.
        migrationBuilder.Sql("""
            CREATE TRIGGER trg_gdtm_khong_sua BEFORE UPDATE ON giao_dich_tien_mat
            FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Khong duoc sua lich su quy tien mat';
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER trg_gdtm_khong_xoa BEFORE DELETE ON giao_dich_tien_mat
            FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Khong duoc xoa lich su quy tien mat';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Refuse rollback once new business events exist; do not erase the audit trail.
        migrationBuilder.Sql("""
            CREATE TEMPORARY TABLE cash_rollback_guard (
              allowed TINYINT NOT NULL,
              CONSTRAINT ck_cash_rollback_no_new_events CHECK (allowed=1));
            """);
        migrationBuilder.Sql("""
            INSERT INTO cash_rollback_guard
            SELECT IF(EXISTS(SELECT 1 FROM giao_dich_tien_mat
              WHERE loai IN ('CHI_HUY_HOA_DON','CHOT_SO_DU_DAU')),0,1);
            """);
        migrationBuilder.Sql("DROP TEMPORARY TABLE cash_rollback_guard;");
        migrationBuilder.Sql("DROP TRIGGER trg_gdtm_khong_sua;");
        migrationBuilder.Sql("DROP TRIGGER trg_gdtm_khong_xoa;");
        migrationBuilder.Sql("""
            ALTER TABLE giao_dich_tien_mat
              DROP CHECK ck_gdtm_so_tien, DROP CHECK ck_gdtm_lien_ket,
              DROP INDEX uq_gdtm_loai_hoa_don, DROP INDEX uq_gdtm_loai_phieu,
              DROP COLUMN ghi_chu,
              MODIFY loai ENUM('THU_BAN_HANG','CHI_HOAN_TRA') NOT NULL,
              ADD CONSTRAINT ck_gdtm_so_tien CHECK (so_tien > 0),
              ADD CONSTRAINT ck_gdtm_lien_ket CHECK (
                (loai='THU_BAN_HANG' AND hoa_don_id IS NOT NULL AND phieu_doi_tra_id IS NULL)
                OR (loai='CHI_HOAN_TRA' AND hoa_don_id IS NULL AND phieu_doi_tra_id IS NOT NULL));
            """);
        migrationBuilder.Sql("ALTER TABLE quy_tien_mat DROP COLUMN da_chot_so_du_dau;");
    }
}
