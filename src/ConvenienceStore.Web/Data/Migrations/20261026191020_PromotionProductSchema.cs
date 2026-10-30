using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConvenienceStore.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class PromotionProductSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "khuyen_mai_san_pham",
                columns: table => new
                {
                    chuong_trinh_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    san_pham_id = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_khuyen_mai_san_pham", x => new { x.chuong_trinh_id, x.san_pham_id });
                    table.ForeignKey(
                        name: "fk_kmsp_ct",
                        column: x => x.chuong_trinh_id,
                        principalTable: "chuong_trinh_khuyen_mai",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_kmsp_sp",
                        column: x => x.san_pham_id,
                        principalTable: "san_pham",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "idx_kmsp_san_pham",
                table: "khuyen_mai_san_pham",
                column: "san_pham_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "khuyen_mai_san_pham");
        }
    }
}
