using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace ConvenienceStore.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class InventoryCountReceiptSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "phieu_kiem_ke",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ma_phieu = table.Column<string>(type: "varchar(20)", nullable: false),
                    pham_vi = table.Column<string>(type: "enum('TOAN_BO','DANH_MUC','CHON_SAN_PHAM')", nullable: false),
                    danh_muc_id = table.Column<uint>(type: "int unsigned", nullable: true),
                    trang_thai = table.Column<string>(type: "enum('NHAP','CHO_DUYET','BI_TU_CHOI','DA_DIEU_CHINH')", nullable: false, defaultValue: "NHAP"),
                    ghi_chu = table.Column<string>(type: "varchar(255)", nullable: true),
                    nguoi_lap_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    ngay_lap = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    gui_duyet_luc = table.Column<DateTime>(type: "datetime", nullable: true),
                    nguoi_duyet_id = table.Column<uint>(type: "int unsigned", nullable: true),
                    duyet_luc = table.Column<DateTime>(type: "datetime", nullable: true),
                    ly_do_tu_choi = table.Column<string>(type: "varchar(255)", nullable: true),
                    phien_ban = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 1u)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_phieu_kiem_ke", x => x.id);
                    table.CheckConstraint("ck_pkk_danh_muc", "(pham_vi = 'DANH_MUC') = (danh_muc_id IS NOT NULL)");
                    table.CheckConstraint("ck_pkk_duyet", "trang_thai NOT IN ('BI_TU_CHOI','DA_DIEU_CHINH') OR (nguoi_duyet_id IS NOT NULL AND duyet_luc IS NOT NULL)");
                    table.CheckConstraint("ck_pkk_tu_choi", "trang_thai <> 'BI_TU_CHOI' OR ly_do_tu_choi IS NOT NULL");
                    table.ForeignKey(
                        name: "fk_pkk_danh_muc",
                        column: x => x.danh_muc_id,
                        principalTable: "danh_muc",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pkk_nguoi_duyet",
                        column: x => x.nguoi_duyet_id,
                        principalTable: "tai_khoan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pkk_nguoi_lap",
                        column: x => x.nguoi_lap_id,
                        principalTable: "tai_khoan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "idx_phieu_kiem_ke_tt",
                table: "phieu_kiem_ke",
                column: "trang_thai");

            migrationBuilder.CreateIndex(
                name: "IX_phieu_kiem_ke_danh_muc_id",
                table: "phieu_kiem_ke",
                column: "danh_muc_id");

            migrationBuilder.CreateIndex(
                name: "IX_phieu_kiem_ke_nguoi_duyet_id",
                table: "phieu_kiem_ke",
                column: "nguoi_duyet_id");

            migrationBuilder.CreateIndex(
                name: "IX_phieu_kiem_ke_nguoi_lap_id",
                table: "phieu_kiem_ke",
                column: "nguoi_lap_id");

            migrationBuilder.CreateIndex(
                name: "uq_phieu_kiem_ke_ma",
                table: "phieu_kiem_ke",
                column: "ma_phieu",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "phieu_kiem_ke");
        }
    }
}
