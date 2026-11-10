using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConvenienceStore.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class InventoryCountDetailSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ct_kiem_ke",
                columns: table => new
                {
                    phieu_kiem_ke_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    san_pham_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    ton_luc_lap = table.Column<int>(type: "int", nullable: false),
                    so_luong_thuc_te = table.Column<int>(type: "int", nullable: true),
                    ly_do_chenh_lech = table.Column<string>(type: "varchar(255)", nullable: true),
                    chenh_lech_luc_duyet = table.Column<int>(type: "int", nullable: true),
                    gia_von_luc_duyet = table.Column<decimal>(type: "decimal(15,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ct_kiem_ke", x => new { x.phieu_kiem_ke_id, x.san_pham_id });
                    table.CheckConstraint("ck_ctkk_thuc_te", "so_luong_thuc_te IS NULL OR so_luong_thuc_te >= 0");
                    table.ForeignKey(
                        name: "fk_ctkk_phieu",
                        column: x => x.phieu_kiem_ke_id,
                        principalTable: "phieu_kiem_ke",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ctkk_san_pham",
                        column: x => x.san_pham_id,
                        principalTable: "san_pham",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "idx_ctkk_san_pham",
                table: "ct_kiem_ke",
                column: "san_pham_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ct_kiem_ke");
        }
    }
}
