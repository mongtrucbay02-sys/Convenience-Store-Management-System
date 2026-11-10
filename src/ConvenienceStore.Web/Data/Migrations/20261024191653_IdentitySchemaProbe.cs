using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace ConvenienceStore.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class IdentitySchemaProbe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "vai_tro",
                columns: table => new
                {
                    id = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    ma = table.Column<string>(type: "char(2)", nullable: false),
                    ten = table.Column<string>(type: "varchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vai_tro", x => x.id);
                    table.CheckConstraint("ck_vai_tro_ma", "ma IN ('AD','TN','NK')");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "tai_khoan",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ten_dang_nhap = table.Column<string>(type: "varchar(50)", nullable: false),
                    mat_khau_bam = table.Column<string>(type: "varchar(255)", nullable: false),
                    ho_ten = table.Column<string>(type: "varchar(100)", nullable: false),
                    so_dien_thoai = table.Column<string>(type: "varchar(10)", nullable: true),
                    email = table.Column<string>(type: "varchar(100)", nullable: true),
                    vai_tro_id = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    trang_thai = table.Column<string>(type: "enum('HOAT_DONG','DA_KHOA')", nullable: false, defaultValue: "HOAT_DONG"),
                    ly_do_khoa = table.Column<string>(type: "varchar(255)", nullable: true),
                    bat_buoc_doi_mk = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    so_lan_sai = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0),
                    khoa_tam_den = table.Column<DateTime>(type: "datetime", nullable: true),
                    lan_dang_nhap_cuoi = table.Column<DateTime>(type: "datetime", nullable: true),
                    phien_ban = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 1u),
                    ngay_tao = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ngay_cap_nhat = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tai_khoan", x => x.id);
                    table.CheckConstraint("ck_tai_khoan_khoa", "trang_thai <> 'DA_KHOA' OR ly_do_khoa IS NOT NULL");
                    table.CheckConstraint("ck_tai_khoan_sdt", "so_dien_thoai IS NULL OR REGEXP_LIKE(so_dien_thoai, '^0[0-9]{9}$')");
                    table.CheckConstraint("ck_tai_khoan_ten", "REGEXP_LIKE(ten_dang_nhap, '^[A-Za-z0-9._]{3,50}$')");
                    table.ForeignKey(
                        name: "fk_tai_khoan_vai_tro",
                        column: x => x.vai_tro_id,
                        principalTable: "vai_tro",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "phien_dang_nhap",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    tai_khoan_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    token_bam = table.Column<string>(type: "char(64)", nullable: false),
                    ngay_tao = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    hoat_dong_cuoi = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    huy_luc = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_phien_dang_nhap", x => x.id);
                    table.ForeignKey(
                        name: "fk_phien_tai_khoan",
                        column: x => x.tai_khoan_id,
                        principalTable: "tai_khoan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "idx_phien_tai_khoan",
                table: "phien_dang_nhap",
                columns: new[] { "tai_khoan_id", "huy_luc" });

            migrationBuilder.CreateIndex(
                name: "uq_phien_token",
                table: "phien_dang_nhap",
                column: "token_bam",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_tai_khoan_vai_tro",
                table: "tai_khoan",
                columns: new[] { "vai_tro_id", "trang_thai" });

            migrationBuilder.CreateIndex(
                name: "uq_tai_khoan_email",
                table: "tai_khoan",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_tai_khoan_sdt",
                table: "tai_khoan",
                column: "so_dien_thoai",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_tai_khoan_ten",
                table: "tai_khoan",
                column: "ten_dang_nhap",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_vai_tro_ma",
                table: "vai_tro",
                column: "ma",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "phien_dang_nhap");

            migrationBuilder.DropTable(
                name: "tai_khoan");

            migrationBuilder.DropTable(
                name: "vai_tro");
        }
    }
}
