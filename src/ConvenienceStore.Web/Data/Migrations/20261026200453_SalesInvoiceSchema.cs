using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace ConvenienceStore.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class SalesInvoiceSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "hoa_don",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ma_hoa_don = table.Column<string>(type: "varchar(20)", nullable: false),
                    gio_hang_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    thu_ngan_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    khach_hang_id = table.Column<uint>(type: "int unsigned", nullable: true),
                    thanh_toan_luc = table.Column<DateTime>(type: "datetime", nullable: false),
                    tong_tien_hang = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    tong_giam = table.Column<ulong>(type: "bigint unsigned", nullable: false, defaultValue: 0ul),
                    tong_thanh_toan = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    tien_tru_hang_doi = table.Column<ulong>(type: "bigint unsigned", nullable: false, defaultValue: 0ul),
                    tien_khach_dua = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    tien_thua = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    so_tien_mot_diem = table.Column<uint>(type: "int unsigned", nullable: true),
                    diem_cong = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    trang_thai = table.Column<string>(type: "enum('DA_THANH_TOAN','DA_HUY')", nullable: false, defaultValue: "DA_THANH_TOAN"),
                    nguoi_huy_id = table.Column<uint>(type: "int unsigned", nullable: true),
                    huy_luc = table.Column<DateTime>(type: "datetime", nullable: true),
                    ly_do_huy = table.Column<string>(type: "varchar(255)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hoa_don", x => x.id);
                    table.CheckConstraint("ck_hd_diem", "khach_hang_id IS NOT NULL OR (diem_cong = 0 AND so_tien_mot_diem IS NULL)");
                    table.CheckConstraint("ck_hd_huy", "trang_thai <> 'DA_HUY' OR (nguoi_huy_id IS NOT NULL AND huy_luc IS NOT NULL AND ly_do_huy IS NOT NULL)");
                    table.CheckConstraint("ck_hd_tien_thua", "tien_khach_dua + tien_tru_hang_doi = tong_thanh_toan + tien_thua");
                    table.CheckConstraint("ck_hd_tong", "tong_thanh_toan + tong_giam = tong_tien_hang");
                    table.CheckConstraint("ck_hd_tru_doi", "tien_tru_hang_doi <= tong_thanh_toan");
                    table.ForeignKey(
                        name: "fk_hd_gio",
                        column: x => x.gio_hang_id,
                        principalTable: "gio_hang",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_hd_khach",
                        column: x => x.khach_hang_id,
                        principalTable: "khach_hang",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_hd_nguoi_huy",
                        column: x => x.nguoi_huy_id,
                        principalTable: "tai_khoan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_hd_thu_ngan",
                        column: x => x.thu_ngan_id,
                        principalTable: "tai_khoan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "idx_hoa_don_huy",
                table: "hoa_don",
                column: "huy_luc");

            migrationBuilder.CreateIndex(
                name: "idx_hoa_don_khach",
                table: "hoa_don",
                columns: new[] { "khach_hang_id", "thanh_toan_luc" });

            migrationBuilder.CreateIndex(
                name: "idx_hoa_don_thu_ngan",
                table: "hoa_don",
                columns: new[] { "thu_ngan_id", "thanh_toan_luc" });

            migrationBuilder.CreateIndex(
                name: "idx_hoa_don_tt_tg",
                table: "hoa_don",
                columns: new[] { "trang_thai", "thanh_toan_luc" });

            migrationBuilder.CreateIndex(
                name: "IX_hoa_don_nguoi_huy_id",
                table: "hoa_don",
                column: "nguoi_huy_id");

            migrationBuilder.CreateIndex(
                name: "uq_hoa_don_gio",
                table: "hoa_don",
                column: "gio_hang_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_hoa_don_ma",
                table: "hoa_don",
                column: "ma_hoa_don",
                unique: true);
            // BR-POS-12: Prevent invoice deletion.
            migrationBuilder.Sql("""
                CREATE TRIGGER trg_hoa_don_khong_xoa
                BEFORE DELETE ON hoa_don
                FOR EACH ROW
                SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'BR-POS-12: hoa_don khong duoc xoa'
                """);

            // BR-POS-07: Reference-defined cancellation transition.
            migrationBuilder.Sql("""
                CREATE TRIGGER trg_hoa_don_chi_huy
                BEFORE UPDATE ON hoa_don
                FOR EACH ROW
                BEGIN
                    IF OLD.trang_thai <> 'DA_THANH_TOAN'
                       OR NEW.trang_thai <> 'DA_HUY'
                       OR NOT (NEW.ma_hoa_don <=> OLD.ma_hoa_don)
                       OR NOT (NEW.thu_ngan_id <=> OLD.thu_ngan_id)
                       OR NOT (NEW.khach_hang_id <=> OLD.khach_hang_id)
                       OR NOT (NEW.thanh_toan_luc <=> OLD.thanh_toan_luc)
                       OR NEW.tong_tien_hang <> OLD.tong_tien_hang
                       OR NEW.tong_giam <> OLD.tong_giam
                       OR NEW.tong_thanh_toan <> OLD.tong_thanh_toan
                       OR NEW.tien_khach_dua <> OLD.tien_khach_dua
                       OR NEW.tien_tru_hang_doi <> OLD.tien_tru_hang_doi
                       OR NEW.tien_thua <> OLD.tien_thua
                       OR NEW.diem_cong <> OLD.diem_cong THEN
                        SIGNAL SQLSTATE '45000'
                        SET MESSAGE_TEXT =
                            'BR-POS-07: hoa_don chi duoc chuyen sang DA_HUY';
                    END IF;
                END
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP TRIGGER IF EXISTS trg_hoa_don_chi_huy");

            migrationBuilder.Sql(
                "DROP TRIGGER IF EXISTS trg_hoa_don_khong_xoa");

            migrationBuilder.DropTable(
                name: "hoa_don");
        }
    }
}
