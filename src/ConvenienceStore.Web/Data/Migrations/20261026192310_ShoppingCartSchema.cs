using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace ConvenienceStore.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class ShoppingCartSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Provider SQL omits STORED for this generated column.
            // Create the table directly with the reference DDL.
            migrationBuilder.Sql("""
                CREATE TABLE `gio_hang` (
                    `id` INT UNSIGNED NOT NULL AUTO_INCREMENT,
                    `thu_ngan_id` INT UNSIGNED NOT NULL,
                    `khach_hang_id` INT UNSIGNED NULL,
                    `trang_thai` ENUM('DANG_BAN','TAM_GIU','DA_HOAN_TAT','DA_HUY') NOT NULL DEFAULT 'DANG_BAN',
                    `ghi_chu_tam_giu` VARCHAR(100) NULL,
                    `tam_giu_luc` DATETIME NULL,
                    `hoan_tat_luc` DATETIME NULL,
                    `huy_luc` DATETIME NULL,
                    `ly_do_huy` ENUM('THU_NGAN_HUY','HET_HAN_TAM_GIU') NULL,
                    `gia_tri_khi_huy` BIGINT UNSIGNED NULL,
                    `phien_ban` INT UNSIGNED NOT NULL DEFAULT 1,
                    `ngay_tao` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    `ngay_cap_nhat` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                    `dang_ban_cua_thu_ngan` INT UNSIGNED AS (
                        IF(trang_thai = 'DANG_BAN', thu_ngan_id, NULL)
                    ) STORED,
                    PRIMARY KEY (`id`),
                    CONSTRAINT `ck_gio_huy` CHECK (
                        trang_thai <> 'DA_HUY' OR
                        (huy_luc IS NOT NULL AND ly_do_huy IS NOT NULL
                         AND gia_tri_khi_huy IS NOT NULL)
                    ),
                    CONSTRAINT `ck_gio_tam_giu` CHECK (
                        trang_thai <> 'TAM_GIU' OR tam_giu_luc IS NOT NULL
                    ),
                    CONSTRAINT `fk_gio_khach` FOREIGN KEY (`khach_hang_id`)
                        REFERENCES `khach_hang` (`id`) ON DELETE RESTRICT,
                    CONSTRAINT `fk_gio_thu_ngan` FOREIGN KEY (`thu_ngan_id`)
                        REFERENCES `tai_khoan` (`id`) ON DELETE RESTRICT
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
                  COLLATE=utf8mb4_0900_ai_ci;
                """);
            migrationBuilder.CreateIndex(
                name: "idx_gio_huy",
                table: "gio_hang",
                columns: new[] { "trang_thai", "huy_luc" });

            migrationBuilder.CreateIndex(
                name: "idx_gio_khach",
                table: "gio_hang",
                column: "khach_hang_id");

            migrationBuilder.CreateIndex(
                name: "idx_gio_thu_ngan",
                table: "gio_hang",
                columns: new[] { "thu_ngan_id", "trang_thai" });

            migrationBuilder.CreateIndex(
                name: "uq_gio_dang_ban",
                table: "gio_hang",
                column: "dang_ban_cua_thu_ngan",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gio_hang");
        }
    }
}
