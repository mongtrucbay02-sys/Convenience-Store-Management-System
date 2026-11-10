using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace ConvenienceStore.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class LoyaltyRuleSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "quy_tac_tich_diem",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    so_tien_mot_diem = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 10000u),
                    dang_bat = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    hieu_luc_tu = table.Column<DateTime>(type: "datetime", nullable: false),
                    nguoi_tao_id = table.Column<uint>(type: "int unsigned", nullable: true),
                    ngay_tao = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quy_tac_tich_diem", x => x.id);
                    table.CheckConstraint("ck_qttd_so_tien", "so_tien_mot_diem > 0");
                    table.ForeignKey(
                        name: "fk_qttd_nguoi_tao",
                        column: x => x.nguoi_tao_id,
                        principalTable: "tai_khoan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "idx_qttd_hieu_luc",
                table: "quy_tac_tich_diem",
                column: "hieu_luc_tu");

            migrationBuilder.CreateIndex(
                name: "IX_quy_tac_tich_diem_nguoi_tao_id",
                table: "quy_tac_tich_diem",
                column: "nguoi_tao_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "quy_tac_tich_diem");
        }
    }
}
