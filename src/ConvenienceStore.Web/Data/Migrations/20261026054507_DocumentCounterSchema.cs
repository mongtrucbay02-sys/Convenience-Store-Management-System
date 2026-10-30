using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConvenienceStore.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class DocumentCounterSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bo_dem_chung_tu",
                columns: table => new
                {
                    tien_to = table.Column<string>(type: "varchar(3)", nullable: false),
                    ky = table.Column<string>(type: "char(8)", nullable: false, defaultValue: ""),
                    so_cuoi = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bo_dem_chung_tu", x => new { x.tien_to, x.ky });
                    table.CheckConstraint("ck_bo_dem_tien_to", "tien_to IN ('PN','HD','TH','KK','XH','SP','NCC','KM')");
                })
                .Annotation("MySQL:Charset", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bo_dem_chung_tu");
        }
    }
}
