using ConvenienceStore.Web.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConvenienceStore.Web.Data.Migrations;

[DbContext(typeof(CuaHangDbContext))]
[Migration("20261112160000_Pos12CancelInvoice")]
public sealed class Pos12CancelInvoice : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE hoan_tien_hoa_don (
                id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
                hoa_don_id INT UNSIGNED NOT NULL,
                so_tien_hoan BIGINT UNSIGNED NOT NULL,
                phuong_thuc ENUM('TIEN_MAT') NOT NULL DEFAULT 'TIEN_MAT',
                diem_da_thu_hoi INT UNSIGNED NOT NULL DEFAULT 0,
                diem_chua_thu_hoi INT UNSIGNED NOT NULL DEFAULT 0,
                ly_do VARCHAR(255) NOT NULL,
                nguoi_thuc_hien_id INT UNSIGNED NOT NULL,
                thoi_diem DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                PRIMARY KEY (id),
                UNIQUE KEY uq_hoan_tien_hd (hoa_don_id),
                KEY idx_hoan_tien_tg (thoi_diem),
                CONSTRAINT fk_hoan_tien_hd FOREIGN KEY (hoa_don_id) REFERENCES hoa_don(id),
                CONSTRAINT fk_hoan_tien_actor FOREIGN KEY (nguoi_thuc_hien_id) REFERENCES tai_khoan(id)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER trg_hoan_tien_hd_khong_sua
            BEFORE UPDATE ON hoan_tien_hoa_don FOR EACH ROW
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'POS-12: hoan_tien_hoa_don chi duoc them';
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER trg_hoan_tien_hd_khong_xoa
            BEFORE DELETE ON hoan_tien_hoa_don FOR EACH ROW
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'POS-12: hoan_tien_hoa_don khong duoc xoa';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_hoan_tien_hd_khong_xoa;");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_hoan_tien_hd_khong_sua;");
        migrationBuilder.Sql("DROP TABLE IF EXISTS hoan_tien_hoa_don;");
    }
}
