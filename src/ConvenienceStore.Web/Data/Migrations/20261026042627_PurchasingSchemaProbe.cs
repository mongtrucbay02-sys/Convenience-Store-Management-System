using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace ConvenienceStore.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class PurchasingSchemaProbe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "nha_cung_cap",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ma = table.Column<string>(type: "varchar(20)", nullable: false),
                    ten = table.Column<string>(type: "varchar(200)", nullable: false),
                    nguoi_lien_he = table.Column<string>(type: "varchar(100)", nullable: true),
                    so_dien_thoai = table.Column<string>(type: "varchar(11)", nullable: false),
                    email = table.Column<string>(type: "varchar(100)", nullable: true),
                    dia_chi = table.Column<string>(type: "varchar(255)", nullable: true),
                    ma_so_thue = table.Column<string>(type: "varchar(14)", nullable: true),
                    ghi_chu = table.Column<string>(type: "varchar(500)", nullable: true),
                    trang_thai = table.Column<string>(type: "enum('HOAT_DONG','NGUNG_HOP_TAC')", nullable: false, defaultValue: "HOAT_DONG"),
                    phien_ban = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 1u),
                    ngay_tao = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ngay_cap_nhat = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nha_cung_cap", x => x.id);
                    table.CheckConstraint("ck_ncc_sdt", "REGEXP_LIKE(so_dien_thoai, '^0[0-9]{9,10}$')");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "phieu_nhap",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ma_phieu = table.Column<string>(type: "varchar(20)", nullable: false),
                    nha_cung_cap_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    so_chung_tu_ncc = table.Column<string>(type: "varchar(50)", nullable: true),
                    ngay_nhap = table.Column<DateOnly>(type: "date", nullable: false),
                    trang_thai = table.Column<string>(type: "enum('NHAP','DA_NHAP_KHO','DA_HUY')", nullable: false, defaultValue: "NHAP"),
                    tong_tien = table.Column<ulong>(type: "bigint unsigned", nullable: false, defaultValue: 0ul),
                    ghi_chu = table.Column<string>(type: "varchar(255)", nullable: true),
                    nguoi_lap_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    ngay_lap = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    nguoi_xac_nhan_id = table.Column<uint>(type: "int unsigned", nullable: true),
                    xac_nhan_luc = table.Column<DateTime>(type: "datetime", nullable: true),
                    nguoi_huy_id = table.Column<uint>(type: "int unsigned", nullable: true),
                    huy_luc = table.Column<DateTime>(type: "datetime", nullable: true),
                    ly_do_huy = table.Column<string>(type: "varchar(255)", nullable: true),
                    phien_ban = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 1u),
                    ngay_cap_nhat = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_phieu_nhap", x => x.id);
                    table.CheckConstraint("ck_pn_huy", "trang_thai <> 'DA_HUY' OR (nguoi_huy_id IS NOT NULL AND huy_luc IS NOT NULL AND ly_do_huy IS NOT NULL)");
                    table.CheckConstraint("ck_pn_xac_nhan", "trang_thai <> 'DA_NHAP_KHO' OR (nguoi_xac_nhan_id IS NOT NULL AND xac_nhan_luc IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_pn_ncc",
                        column: x => x.nha_cung_cap_id,
                        principalTable: "nha_cung_cap",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_pn_nguoi_huy",
                        column: x => x.nguoi_huy_id,
                        principalTable: "tai_khoan",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_pn_nguoi_lap",
                        column: x => x.nguoi_lap_id,
                        principalTable: "tai_khoan",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_pn_nguoi_xac_nhan",
                        column: x => x.nguoi_xac_nhan_id,
                        principalTable: "tai_khoan",
                        principalColumn: "id");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ct_phieu_nhap",
                columns: table => new
                {
                    phieu_nhap_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    san_pham_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    ten_san_pham = table.Column<string>(type: "varchar(200)", nullable: false),
                    don_vi_tinh = table.Column<string>(type: "varchar(20)", nullable: false),
                    so_luong = table.Column<uint>(type: "int unsigned", nullable: false),
                    gia_nhap = table.Column<uint>(type: "int unsigned", nullable: false),
                    thanh_tien = table.Column<ulong>(type: "bigint unsigned", nullable: false, computedColumnSql: "so_luong * gia_nhap", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ct_phieu_nhap", x => new { x.phieu_nhap_id, x.san_pham_id });
                    table.CheckConstraint("ck_ctpn_so_luong", "so_luong > 0");
                    table.ForeignKey(
                        name: "fk_ctpn_phieu",
                        column: x => x.phieu_nhap_id,
                        principalTable: "phieu_nhap",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_ctpn_san_pham",
                        column: x => x.san_pham_id,
                        principalTable: "san_pham",
                        principalColumn: "id");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            // MySql.EntityFrameworkCore omits STORED when generating this column.
            // Keep the generated column consistent with schema_v1.sql.
            migrationBuilder.Sql(
                "ALTER TABLE `ct_phieu_nhap` MODIFY COLUMN `thanh_tien` bigint unsigned GENERATED ALWAYS AS (so_luong * gia_nhap) STORED;");

            migrationBuilder.CreateIndex(
                name: "idx_ctpn_san_pham",
                table: "ct_phieu_nhap",
                column: "san_pham_id");

            migrationBuilder.CreateIndex(
                name: "idx_ncc_ten",
                table: "nha_cung_cap",
                column: "ten");

            migrationBuilder.CreateIndex(
                name: "uq_ncc_ma",
                table: "nha_cung_cap",
                column: "ma",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_ncc_sdt",
                table: "nha_cung_cap",
                column: "so_dien_thoai",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_phieu_nhap_ncc",
                table: "phieu_nhap",
                columns: new[] { "nha_cung_cap_id", "xac_nhan_luc" });

            migrationBuilder.CreateIndex(
                name: "idx_phieu_nhap_tt",
                table: "phieu_nhap",
                columns: new[] { "trang_thai", "ngay_lap" });

            migrationBuilder.CreateIndex(
                name: "idx_phieu_nhap_xac_nhan",
                table: "phieu_nhap",
                column: "xac_nhan_luc");

            migrationBuilder.CreateIndex(
                name: "IX_phieu_nhap_nguoi_huy_id",
                table: "phieu_nhap",
                column: "nguoi_huy_id");

            migrationBuilder.CreateIndex(
                name: "IX_phieu_nhap_nguoi_lap_id",
                table: "phieu_nhap",
                column: "nguoi_lap_id");

            migrationBuilder.CreateIndex(
                name: "IX_phieu_nhap_nguoi_xac_nhan_id",
                table: "phieu_nhap",
                column: "nguoi_xac_nhan_id");

            migrationBuilder.CreateIndex(
                name: "uq_phieu_nhap_ma",
                table: "phieu_nhap",
                column: "ma_phieu",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ct_phieu_nhap");

            migrationBuilder.DropTable(
                name: "phieu_nhap");

            migrationBuilder.DropTable(
                name: "nha_cung_cap");
        }
    }
}
