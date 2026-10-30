using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace ConvenienceStore.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class InventoryLedgerSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "the_kho",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    san_pham_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    loai_bien_dong = table.Column<string>(type: "enum('NHAP_HANG','BAN_HANG','HUY_HOA_DON','TRA_HANG','KIEM_KE','XUAT_HUY')", nullable: false),
                    ma_chung_tu = table.Column<string>(type: "varchar(20)", nullable: false),
                    chung_tu_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    so_luong_thay_doi = table.Column<int>(type: "int", nullable: false),
                    ton_sau = table.Column<int>(type: "int", nullable: false),
                    gia_von_ap_dung = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    gia_von_sau = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    nguoi_thuc_hien_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    thoi_diem = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_the_kho", x => x.id);
                    table.CheckConstraint("ck_the_kho_huong", "(\n                    (loai_bien_dong IN ('NHAP_HANG','HUY_HOA_DON','TRA_HANG')\n                     AND so_luong_thay_doi > 0)\n                    OR\n                    (loai_bien_dong IN ('BAN_HANG','XUAT_HUY')\n                     AND so_luong_thay_doi < 0)\n                    OR\n                    loai_bien_dong = 'KIEM_KE'\n                  )");
                    table.CheckConstraint("ck_the_kho_khac_0", "so_luong_thay_doi <> 0");
                    table.CheckConstraint("ck_the_kho_ton_sau", "ton_sau >= 0");
                    table.ForeignKey(
                        name: "fk_the_kho_nguoi",
                        column: x => x.nguoi_thuc_hien_id,
                        principalTable: "tai_khoan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_the_kho_san_pham",
                        column: x => x.san_pham_id,
                        principalTable: "san_pham",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "idx_the_kho_chung_tu",
                table: "the_kho",
                columns: new[] { "loai_bien_dong", "chung_tu_id" });

            migrationBuilder.CreateIndex(
                name: "idx_the_kho_sp_tg",
                table: "the_kho",
                columns: new[] { "san_pham_id", "thoi_diem" });

            migrationBuilder.CreateIndex(
                name: "idx_the_kho_tg",
                table: "the_kho",
                column: "thoi_diem");

            migrationBuilder.CreateIndex(
                name: "IX_the_kho_nguoi_thuc_hien_id",
                table: "the_kho",
                column: "nguoi_thuc_hien_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "the_kho");
        }
    }
}
