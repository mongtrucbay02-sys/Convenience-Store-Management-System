using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace ConvenienceStore.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class CustomerSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "khach_hang",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ho_ten = table.Column<string>(type: "varchar(100)", nullable: true),
                    so_dien_thoai = table.Column<string>(type: "varchar(10)", nullable: true),
                    email = table.Column<string>(type: "varchar(100)", nullable: true),
                    ngay_sinh = table.Column<DateTime>(type: "date", nullable: true),
                    gioi_tinh = table.Column<string>(type: "enum('NAM','NU','KHAC')", nullable: true),
                    diem_hien_co = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    trang_thai = table.Column<string>(type: "enum('HOAT_DONG','DA_AN')", nullable: false, defaultValue: "HOAT_DONG"),
                    xoa_du_lieu_luc = table.Column<DateTime>(type: "datetime", nullable: true),
                    nguoi_tao_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    phien_ban = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 1u),
                    ngay_tao = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ngay_cap_nhat = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_khach_hang", x => x.id);
                    table.CheckConstraint("ck_kh_hoat_dong", "trang_thai <> 'HOAT_DONG' OR (ho_ten IS NOT NULL AND so_dien_thoai IS NOT NULL)");
                    table.CheckConstraint("ck_kh_sdt", "so_dien_thoai IS NULL OR REGEXP_LIKE(so_dien_thoai, '^0[0-9]{9}$')");
                    table.ForeignKey(
                        name: "fk_kh_nguoi_tao",
                        column: x => x.nguoi_tao_id,
                        principalTable: "tai_khoan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "idx_khach_hang_ten",
                table: "khach_hang",
                column: "ho_ten");

            migrationBuilder.CreateIndex(
                name: "IX_khach_hang_nguoi_tao_id",
                table: "khach_hang",
                column: "nguoi_tao_id");

            migrationBuilder.CreateIndex(
                name: "uq_khach_hang_sdt",
                table: "khach_hang",
                column: "so_dien_thoai",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "khach_hang");
        }
    }
}
