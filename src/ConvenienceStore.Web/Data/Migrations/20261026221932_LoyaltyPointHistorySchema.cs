using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace ConvenienceStore.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class LoyaltyPointHistorySchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "lich_su_diem",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    khach_hang_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    loai = table.Column<string>(type: "enum('TICH_DIEM','THU_HOI_TRA_HANG','THU_HOI_HUY_HOA_DON','DIEU_CHINH')", nullable: false),
                    so_diem = table.Column<int>(type: "int", nullable: false),
                    diem_sau = table.Column<uint>(type: "int unsigned", nullable: false),
                    hoa_don_id = table.Column<uint>(type: "int unsigned", nullable: true),
                    phieu_doi_tra_id = table.Column<uint>(type: "int unsigned", nullable: true),
                    ly_do = table.Column<string>(type: "varchar(255)", nullable: true),
                    nguoi_thuc_hien_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    thoi_diem = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lich_su_diem", x => x.id);
                    table.CheckConstraint("ck_lsd_huong", "(loai = 'TICH_DIEM' AND so_diem > 0 AND hoa_don_id IS NOT NULL) OR (loai = 'THU_HOI_TRA_HANG' AND so_diem < 0 AND phieu_doi_tra_id IS NOT NULL) OR (loai = 'THU_HOI_HUY_HOA_DON' AND so_diem < 0 AND hoa_don_id IS NOT NULL) OR (loai = 'DIEU_CHINH' AND ly_do IS NOT NULL)");
                    table.CheckConstraint("ck_lsd_khac_0", "so_diem <> 0");
                    table.ForeignKey(
                        name: "fk_lsd_hd",
                        column: x => x.hoa_don_id,
                        principalTable: "hoa_don",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_lsd_khach",
                        column: x => x.khach_hang_id,
                        principalTable: "khach_hang",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_lsd_nguoi",
                        column: x => x.nguoi_thuc_hien_id,
                        principalTable: "tai_khoan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_lsd_pdt",
                        column: x => x.phieu_doi_tra_id,
                        principalTable: "phieu_doi_tra",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "idx_lsd_hoa_don",
                table: "lich_su_diem",
                column: "hoa_don_id");

            migrationBuilder.CreateIndex(
                name: "idx_lsd_khach_tg",
                table: "lich_su_diem",
                columns: new[] { "khach_hang_id", "thoi_diem" });

            migrationBuilder.CreateIndex(
                name: "IX_lich_su_diem_nguoi_thuc_hien_id",
                table: "lich_su_diem",
                column: "nguoi_thuc_hien_id");

            migrationBuilder.CreateIndex(
                name: "IX_lich_su_diem_phieu_doi_tra_id",
                table: "lich_su_diem",
                column: "phieu_doi_tra_id");
            // BR-KH-02: Point history is append-only.
            migrationBuilder.Sql("""
                CREATE TRIGGER trg_lsd_khong_sua
                BEFORE UPDATE ON lich_su_diem
                FOR EACH ROW
                SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'BR-KH-02: lich_su_diem khong duoc sua'
                """);

            migrationBuilder.Sql("""
                CREATE TRIGGER trg_lsd_khong_xoa
                BEFORE DELETE ON lich_su_diem
                FOR EACH ROW
                SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'BR-KH-02: lich_su_diem khong duoc xoa'
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP TRIGGER IF EXISTS trg_lsd_khong_xoa");

            migrationBuilder.Sql(
                "DROP TRIGGER IF EXISTS trg_lsd_khong_sua");

            migrationBuilder.DropTable(
                name: "lich_su_diem");
        }
    }
}
