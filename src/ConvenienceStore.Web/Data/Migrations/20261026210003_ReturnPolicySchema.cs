using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace ConvenienceStore.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReturnPolicySchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "chinh_sach_doi_tra",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    so_ngay_doi_tra = table.Column<ushort>(type: "smallint unsigned", nullable: false, defaultValue: (ushort)7),
                    nguong_tien_can_duyet = table.Column<ulong>(type: "bigint unsigned", nullable: false, defaultValue: 500000ul),
                    hieu_luc_tu = table.Column<DateTime>(type: "datetime", nullable: false),
                    nguoi_tao_id = table.Column<uint>(type: "int unsigned", nullable: true),
                    ngay_tao = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chinh_sach_doi_tra", x => x.id);
                    table.ForeignKey(
                        name: "fk_csdt_nguoi_tao",
                        column: x => x.nguoi_tao_id,
                        principalTable: "tai_khoan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "idx_csdt_hieu_luc",
                table: "chinh_sach_doi_tra",
                column: "hieu_luc_tu");

            migrationBuilder.CreateIndex(
                name: "IX_chinh_sach_doi_tra_nguoi_tao_id",
                table: "chinh_sach_doi_tra",
                column: "nguoi_tao_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chinh_sach_doi_tra");
        }
    }
}
