using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace ConvenienceStore.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReturnReceiptSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "phieu_doi_tra",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ma_phieu = table.Column<string>(type: "varchar(20)", nullable: false),
                    loai = table.Column<string>(type: "enum('TRA_HANG','DOI_HANG')", nullable: false),
                    hoa_don_goc_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    hoa_don_moi_id = table.Column<uint>(type: "int unsigned", nullable: true),
                    khach_hang_id = table.Column<uint>(type: "int unsigned", nullable: true),
                    chinh_sach_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    trang_thai = table.Column<string>(type: "enum('CHO_DUYET','DA_DUYET','HOAN_TAT','BI_TU_CHOI')", nullable: false),
                    can_duyet_qua_han = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    can_duyet_danh_muc = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    can_duyet_vuot_nguong = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    gia_tri_hang_tra = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    tien_hoan = table.Column<ulong>(type: "bigint unsigned", nullable: false, defaultValue: 0ul),
                    tien_thu_them = table.Column<ulong>(type: "bigint unsigned", nullable: false, defaultValue: 0ul),
                    diem_thu_hoi = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    thu_ngan_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    ngay_lap = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    nguoi_duyet_id = table.Column<uint>(type: "int unsigned", nullable: true),
                    duyet_luc = table.Column<DateTime>(type: "datetime", nullable: true),
                    ghi_chu_duyet = table.Column<string>(type: "varchar(255)", nullable: true),
                    hoan_tat_luc = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_phieu_doi_tra", x => x.id);
                    table.CheckConstraint("ck_pdt_can_duyet", "NOT (can_duyet_qua_han OR can_duyet_danh_muc OR can_duyet_vuot_nguong) OR trang_thai = 'CHO_DUYET' OR (nguoi_duyet_id IS NOT NULL AND duyet_luc IS NOT NULL)");
                    table.CheckConstraint("ck_pdt_da_duyet", "trang_thai <> 'DA_DUYET' OR loai = 'DOI_HANG'");
                    table.CheckConstraint("ck_pdt_doi", "loai <> 'DOI_HANG' OR (tien_hoan = 0 AND (trang_thai <> 'HOAN_TAT' OR hoa_don_moi_id IS NOT NULL))");
                    table.CheckConstraint("ck_pdt_hoan_tat", "trang_thai <> 'HOAN_TAT' OR hoan_tat_luc IS NOT NULL");
                    table.CheckConstraint("ck_pdt_khong_can_duyet", "can_duyet_qua_han OR can_duyet_danh_muc OR can_duyet_vuot_nguong OR (trang_thai = 'HOAN_TAT' AND nguoi_duyet_id IS NULL)");
                    table.CheckConstraint("ck_pdt_tra", "loai <> 'TRA_HANG' OR (hoa_don_moi_id IS NULL AND tien_thu_them = 0 AND tien_hoan = gia_tri_hang_tra)");
                    table.ForeignKey(
                        name: "fk_pdt_cs",
                        column: x => x.chinh_sach_id,
                        principalTable: "chinh_sach_doi_tra",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pdt_hd_goc",
                        column: x => x.hoa_don_goc_id,
                        principalTable: "hoa_don",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pdt_hd_moi",
                        column: x => x.hoa_don_moi_id,
                        principalTable: "hoa_don",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pdt_khach",
                        column: x => x.khach_hang_id,
                        principalTable: "khach_hang",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pdt_nguoi_duyet",
                        column: x => x.nguoi_duyet_id,
                        principalTable: "tai_khoan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pdt_thu_ngan",
                        column: x => x.thu_ngan_id,
                        principalTable: "tai_khoan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "idx_pdt_hd_goc",
                table: "phieu_doi_tra",
                column: "hoa_don_goc_id");

            migrationBuilder.CreateIndex(
                name: "idx_pdt_thu_ngan",
                table: "phieu_doi_tra",
                columns: new[] { "thu_ngan_id", "ngay_lap" });

            migrationBuilder.CreateIndex(
                name: "idx_pdt_tt",
                table: "phieu_doi_tra",
                columns: new[] { "trang_thai", "hoan_tat_luc" });

            migrationBuilder.CreateIndex(
                name: "IX_phieu_doi_tra_chinh_sach_id",
                table: "phieu_doi_tra",
                column: "chinh_sach_id");

            migrationBuilder.CreateIndex(
                name: "IX_phieu_doi_tra_khach_hang_id",
                table: "phieu_doi_tra",
                column: "khach_hang_id");

            migrationBuilder.CreateIndex(
                name: "IX_phieu_doi_tra_nguoi_duyet_id",
                table: "phieu_doi_tra",
                column: "nguoi_duyet_id");

            migrationBuilder.CreateIndex(
                name: "uq_pdt_hd_moi",
                table: "phieu_doi_tra",
                column: "hoa_don_moi_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_pdt_id_hd_goc",
                table: "phieu_doi_tra",
                columns: new[] { "id", "hoa_don_goc_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_pdt_ma",
                table: "phieu_doi_tra",
                column: "ma_phieu",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "phieu_doi_tra");
        }
    }
}
