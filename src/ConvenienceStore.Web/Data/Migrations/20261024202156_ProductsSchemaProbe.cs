using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace ConvenienceStore.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProductsSchemaProbe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "danh_muc",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ten = table.Column<string>(type: "varchar(100)", nullable: false),
                    mo_ta = table.Column<string>(type: "varchar(255)", nullable: true),
                    khong_doi_tra = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    trang_thai = table.Column<string>(type: "enum('HOAT_DONG','DA_AN')", nullable: false, defaultValue: "HOAT_DONG"),
                    phien_ban = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 1u),
                    ngay_tao = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ngay_cap_nhat = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_danh_muc", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "san_pham",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ma = table.Column<string>(type: "varchar(20)", nullable: false),
                    ten_san_pham = table.Column<string>(type: "varchar(200)", nullable: false),
                    danh_muc_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    don_vi_tinh = table.Column<string>(type: "varchar(20)", nullable: false),
                    anh_url = table.Column<string>(type: "varchar(255)", nullable: true),
                    mo_ta = table.Column<string>(type: "varchar(1000)", nullable: true),
                    trang_thai = table.Column<string>(type: "enum('DANG_KINH_DOANH','NGUNG_KINH_DOANH')", nullable: false, defaultValue: "DANG_KINH_DOANH"),
                    nguoi_tao_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    phien_ban = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 1u),
                    ngay_tao = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ngay_cap_nhat = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_san_pham", x => x.id);
                    table.ForeignKey(
                        name: "fk_san_pham_danh_muc",
                        column: x => x.danh_muc_id,
                        principalTable: "danh_muc",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_san_pham_nguoi_tao",
                        column: x => x.nguoi_tao_id,
                        principalTable: "tai_khoan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "lich_su_gia",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    san_pham_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    gia_ban = table.Column<uint>(type: "int unsigned", nullable: false),
                    hieu_luc_tu = table.Column<DateTime>(type: "datetime", nullable: false),
                    nguoi_tao_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    ngay_tao = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    huy_luc = table.Column<DateTime>(type: "datetime", nullable: true),
                    nguoi_huy_id = table.Column<uint>(type: "int unsigned", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lich_su_gia", x => x.id);
                    table.CheckConstraint("ck_lsg_gia", "gia_ban > 0 AND gia_ban % 100 = 0");
                    table.CheckConstraint("ck_lsg_huy", "(huy_luc IS NULL) = (nguoi_huy_id IS NULL)");
                    table.ForeignKey(
                        name: "fk_lsg_nguoi_huy",
                        column: x => x.nguoi_huy_id,
                        principalTable: "tai_khoan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_lsg_nguoi_tao",
                        column: x => x.nguoi_tao_id,
                        principalTable: "tai_khoan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_lsg_san_pham",
                        column: x => x.san_pham_id,
                        principalTable: "san_pham",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "uq_danh_muc_ten",
                table: "danh_muc",
                column: "ten",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_lsg_san_pham_hieu_luc",
                table: "lich_su_gia",
                columns: new[] { "san_pham_id", "hieu_luc_tu" });

            migrationBuilder.CreateIndex(
                name: "IX_lich_su_gia_nguoi_huy_id",
                table: "lich_su_gia",
                column: "nguoi_huy_id");

            migrationBuilder.CreateIndex(
                name: "IX_lich_su_gia_nguoi_tao_id",
                table: "lich_su_gia",
                column: "nguoi_tao_id");

            migrationBuilder.CreateIndex(
                name: "idx_san_pham_danh_muc",
                table: "san_pham",
                columns: new[] { "danh_muc_id", "trang_thai" });

            migrationBuilder.CreateIndex(
                name: "idx_san_pham_ten",
                table: "san_pham",
                column: "ten_san_pham");

            migrationBuilder.CreateIndex(
                name: "IX_san_pham_nguoi_tao_id",
                table: "san_pham",
                column: "nguoi_tao_id");

            migrationBuilder.CreateIndex(
                name: "uq_san_pham_ma",
                table: "san_pham",
                column: "ma",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lich_su_gia");

            migrationBuilder.DropTable(
                name: "san_pham");

            migrationBuilder.DropTable(
                name: "danh_muc");
        }
    }
}
