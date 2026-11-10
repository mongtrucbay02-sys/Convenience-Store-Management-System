using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConvenienceStore.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class InventoryDisposalDetailSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ct_xuat_huy",
                columns: table => new
                {
                    phieu_xuat_huy_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    san_pham_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    so_luong = table.Column<uint>(type: "int unsigned", nullable: false),
                    ly_do = table.Column<string>(type: "enum('HONG','HET_HAN','MAT','KHAC')", nullable: false),
                    ghi_chu_ly_do = table.Column<string>(type: "varchar(255)", nullable: true),
                    gia_von_luc_duyet = table.Column<decimal>(type: "decimal(15,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ct_xuat_huy", x => new { x.phieu_xuat_huy_id, x.san_pham_id });
                    table.CheckConstraint("ck_ctxh_khac", "ly_do <> 'KHAC' OR ghi_chu_ly_do IS NOT NULL");
                    table.CheckConstraint("ck_ctxh_so_luong", "so_luong > 0");
                    table.ForeignKey(
                        name: "fk_ctxh_phieu",
                        column: x => x.phieu_xuat_huy_id,
                        principalTable: "phieu_xuat_huy",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ctxh_san_pham",
                        column: x => x.san_pham_id,
                        principalTable: "san_pham",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "idx_ctxh_san_pham",
                table: "ct_xuat_huy",
                column: "san_pham_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ct_xuat_huy");
        }
    }
}
