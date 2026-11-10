using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConvenienceStore.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class InventoryStockSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ton_kho",
                columns: table => new
                {
                    san_pham_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    so_luong_ton = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    gia_von_binh_quan = table.Column<decimal>(type: "decimal(15,2)", nullable: false, defaultValue: 0m),
                    nguong_toi_thieu = table.Column<int>(type: "int", nullable: false, defaultValue: 5),
                    ngay_cap_nhat = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ton_kho", x => x.san_pham_id);
                    table.CheckConstraint("ck_gia_von_khong_am", "gia_von_binh_quan >= 0");
                    table.CheckConstraint("ck_nguong_khong_am", "nguong_toi_thieu >= 0");
                    table.CheckConstraint("ck_ton_khong_am", "so_luong_ton >= 0");
                    table.ForeignKey(
                        name: "fk_ton_kho_san_pham",
                        column: x => x.san_pham_id,
                        principalTable: "san_pham",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ton_kho");
        }
    }
}
