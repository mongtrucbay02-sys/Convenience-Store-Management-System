using ConvenienceStore.Web.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConvenienceStore.Web.Data.Migrations;

[DbContext(typeof(CuaHangDbContext))]
[Migration("20261112170000_CashLedger")]
public sealed class CashLedger : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE quy_tien_mat (
                id INT UNSIGNED NOT NULL,
                so_du BIGINT UNSIGNED NOT NULL DEFAULT 0,
                ngay_cap_nhat DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                PRIMARY KEY (id),
                CONSTRAINT ck_quy_tien_mat_so_du CHECK (so_du >= 0)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
            """);

        migrationBuilder.Sql("""
            INSERT INTO quy_tien_mat (id, so_du)
            VALUES (1, 0);
            """);

        migrationBuilder.Sql("""
            CREATE TABLE giao_dich_tien_mat (
                id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
                loai ENUM('THU_BAN_HANG','CHI_HOAN_TRA') NOT NULL,
                hoa_don_id INT UNSIGNED NULL,
                phieu_doi_tra_id INT UNSIGNED NULL,
                so_tien BIGINT UNSIGNED NOT NULL,
                so_du_truoc BIGINT UNSIGNED NOT NULL,
                so_du_sau BIGINT UNSIGNED NOT NULL,
                nguoi_thuc_hien_id INT UNSIGNED NOT NULL,
                thoi_diem DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                PRIMARY KEY (id),
                KEY idx_gdtm_hoa_don (hoa_don_id),
                KEY idx_gdtm_phieu (phieu_doi_tra_id),
                KEY idx_gdtm_actor (nguoi_thuc_hien_id),
                CONSTRAINT ck_gdtm_so_tien CHECK (so_tien > 0),
                CONSTRAINT ck_gdtm_lien_ket CHECK (
                    (loai = 'THU_BAN_HANG' AND hoa_don_id IS NOT NULL AND phieu_doi_tra_id IS NULL)
                    OR
                    (loai = 'CHI_HOAN_TRA' AND hoa_don_id IS NULL AND phieu_doi_tra_id IS NOT NULL)
                ),
                CONSTRAINT fk_gdtm_hoa_don FOREIGN KEY (hoa_don_id) REFERENCES hoa_don (id),
                CONSTRAINT fk_gdtm_phieu FOREIGN KEY (phieu_doi_tra_id) REFERENCES phieu_doi_tra (id),
                CONSTRAINT fk_gdtm_actor FOREIGN KEY (nguoi_thuc_hien_id) REFERENCES tai_khoan (id)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS giao_dich_tien_mat;");
        migrationBuilder.Sql("DROP TABLE IF EXISTS quy_tien_mat;");
    }
}
