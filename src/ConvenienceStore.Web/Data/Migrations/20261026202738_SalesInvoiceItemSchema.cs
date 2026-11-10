using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConvenienceStore.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class SalesInvoiceItemSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // MySql.EntityFrameworkCore omits STORED in generated SQL.
            // Create the generated column as STORED from the outset.
            migrationBuilder.Sql("""
                CREATE TABLE `ct_hoa_don` (
                    `hoa_don_id` INT UNSIGNED NOT NULL,
                    `san_pham_id` INT UNSIGNED NOT NULL,
                    `ten_san_pham` VARCHAR(200) NOT NULL,
                    `don_vi_tinh` VARCHAR(20) NOT NULL,
                    `so_luong` INT UNSIGNED NOT NULL,
                    `don_gia` INT UNSIGNED NOT NULL,
                    `khuyen_mai_id` INT UNSIGNED NULL,
                    `giam_don_vi` INT UNSIGNED NOT NULL DEFAULT 0,
                    `gia_von` DECIMAL(15,2) NOT NULL,
                    `thanh_tien` BIGINT UNSIGNED
                        AS (so_luong * (don_gia - giam_don_vi)) STORED,
                    PRIMARY KEY (`hoa_don_id`, `san_pham_id`),
                    CONSTRAINT `ck_cthd_so_luong`
                        CHECK (so_luong > 0),
                    CONSTRAINT `ck_cthd_giam`
                        CHECK (giam_don_vi <= don_gia
                               AND giam_don_vi % 100 = 0),
                    CONSTRAINT `ck_cthd_km`
                        CHECK (khuyen_mai_id IS NOT NULL
                               OR giam_don_vi = 0),
                    CONSTRAINT `fk_cthd_hd`
                        FOREIGN KEY (`hoa_don_id`)
                        REFERENCES `hoa_don` (`id`)
                        ON DELETE RESTRICT,
                    CONSTRAINT `fk_cthd_san_pham`
                        FOREIGN KEY (`san_pham_id`)
                        REFERENCES `san_pham` (`id`)
                        ON DELETE RESTRICT,
                    CONSTRAINT `fk_cthd_km`
                        FOREIGN KEY (`khuyen_mai_id`)
                        REFERENCES `chuong_trinh_khuyen_mai` (`id`)
                        ON DELETE RESTRICT
                ) ENGINE=InnoDB
                  DEFAULT CHARSET=utf8mb4
                  COLLATE=utf8mb4_0900_ai_ci
                """);

            migrationBuilder.CreateIndex(
                name: "idx_cthd_km",
                table: "ct_hoa_don",
                column: "khuyen_mai_id");

            migrationBuilder.CreateIndex(
                name: "idx_cthd_san_pham",
                table: "ct_hoa_don",
                column: "san_pham_id");
            // Invoice lines are immutable after creation.
            migrationBuilder.Sql("""
                CREATE TRIGGER trg_ct_hoa_don_khong_sua
                BEFORE UPDATE ON ct_hoa_don
                FOR EACH ROW
                SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'BR-POS-07: ct_hoa_don khong duoc sua'
                """);

            migrationBuilder.Sql("""
                CREATE TRIGGER trg_ct_hoa_don_khong_xoa
                BEFORE DELETE ON ct_hoa_don
                FOR EACH ROW
                SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'BR-POS-07: ct_hoa_don khong duoc xoa'
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP TRIGGER IF EXISTS trg_ct_hoa_don_khong_xoa");

            migrationBuilder.Sql(
                "DROP TRIGGER IF EXISTS trg_ct_hoa_don_khong_sua");

            migrationBuilder.DropTable(
                name: "ct_hoa_don");
        }
    }
}
