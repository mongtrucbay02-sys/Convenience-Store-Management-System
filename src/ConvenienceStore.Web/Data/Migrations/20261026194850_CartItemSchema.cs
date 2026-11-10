using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConvenienceStore.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class CartItemSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ct_gio_hang",
                columns: table => new
                {
                    gio_hang_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    san_pham_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    so_luong = table.Column<uint>(type: "int unsigned", nullable: false),
                    don_gia = table.Column<uint>(type: "int unsigned", nullable: false),
                    khuyen_mai_id = table.Column<uint>(type: "int unsigned", nullable: true),
                    giam_don_vi = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    them_luc = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ct_gio_hang", x => new { x.gio_hang_id, x.san_pham_id });
                    table.CheckConstraint("ck_ctgh_giam", "giam_don_vi <= don_gia AND giam_don_vi % 100 = 0");
                    table.CheckConstraint("ck_ctgh_km", "khuyen_mai_id IS NOT NULL OR giam_don_vi = 0");
                    table.CheckConstraint("ck_ctgh_so_luong", "so_luong > 0");
                    table.ForeignKey(
                        name: "fk_ctgh_gio",
                        column: x => x.gio_hang_id,
                        principalTable: "gio_hang",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ctgh_km",
                        column: x => x.khuyen_mai_id,
                        principalTable: "chuong_trinh_khuyen_mai",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ctgh_san_pham",
                        column: x => x.san_pham_id,
                        principalTable: "san_pham",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "idx_ctgh_san_pham",
                table: "ct_gio_hang",
                column: "san_pham_id");

            migrationBuilder.CreateIndex(
                name: "IX_ct_gio_hang_khuyen_mai_id",
                table: "ct_gio_hang",
                column: "khuyen_mai_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ct_gio_hang");
        }
    }
}
