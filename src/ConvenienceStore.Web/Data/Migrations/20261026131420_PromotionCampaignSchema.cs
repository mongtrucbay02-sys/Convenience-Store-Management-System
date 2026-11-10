using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace ConvenienceStore.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class PromotionCampaignSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "chuong_trinh_khuyen_mai",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ma = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    ten = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    mo_ta = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    loai_giam = table.Column<string>(type: "enum('PHAN_TRAM','SO_TIEN')", nullable: false),
                    gia_tri = table.Column<uint>(type: "int unsigned", nullable: false),
                    giam_toi_da = table.Column<uint>(type: "int unsigned", nullable: true),
                    bat_dau_luc = table.Column<DateTime>(type: "datetime", nullable: false),
                    ket_thuc_luc = table.Column<DateTime>(type: "datetime", nullable: false),
                    chi_thanh_vien = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    trang_thai = table.Column<string>(type: "enum('NHAP','DA_KICH_HOAT','TAM_DUNG','DA_KET_THUC')", nullable: false, defaultValue: "NHAP"),
                    nguoi_tao_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    phien_ban = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 1u),
                    ngay_tao = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ngay_cap_nhat = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chuong_trinh_khuyen_mai", x => x.id);
                    table.CheckConstraint("ck_ctkm_gia_tri", "(loai_giam = 'PHAN_TRAM' AND gia_tri BETWEEN 1 AND 100 AND (giam_toi_da IS NULL OR (giam_toi_da > 0 AND giam_toi_da % 100 = 0))) OR (loai_giam = 'SO_TIEN' AND gia_tri > 0 AND gia_tri % 100 = 0 AND giam_toi_da IS NULL)");
                    table.CheckConstraint("ck_ctkm_thoi_gian", "ket_thuc_luc > bat_dau_luc");
                    table.ForeignKey(
                        name: "fk_ctkm_nguoi_tao",
                        column: x => x.nguoi_tao_id,
                        principalTable: "tai_khoan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "idx_ctkm_tt",
                table: "chuong_trinh_khuyen_mai",
                columns: new[] { "trang_thai", "bat_dau_luc", "ket_thuc_luc" });

            migrationBuilder.CreateIndex(
                name: "IX_chuong_trinh_khuyen_mai_nguoi_tao_id",
                table: "chuong_trinh_khuyen_mai",
                column: "nguoi_tao_id");

            migrationBuilder.CreateIndex(
                name: "uq_ctkm_ma",
                table: "chuong_trinh_khuyen_mai",
                column: "ma",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chuong_trinh_khuyen_mai");
        }
    }
}
