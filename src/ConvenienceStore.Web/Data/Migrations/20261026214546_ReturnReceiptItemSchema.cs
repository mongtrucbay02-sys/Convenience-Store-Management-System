using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConvenienceStore.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReturnReceiptItemSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ct_phieu_doi_tra",
                columns: table => new
                {
                    phieu_doi_tra_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    san_pham_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    hoa_don_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    so_luong = table.Column<uint>(type: "int unsigned", nullable: false),
                    don_gia_thuc_tra = table.Column<uint>(type: "int unsigned", nullable: false),
                    ly_do = table.Column<string>(type: "enum('LOI_SAN_PHAM','HET_HAN','KHONG_DUNG_NHU_CAU','KHAC')", nullable: false),
                    ghi_chu_ly_do = table.Column<string>(type: "varchar(255)", nullable: true),
                    tinh_trang = table.Column<string>(type: "varchar(255)", nullable: true),
                    cach_xu_ly = table.Column<string>(type: "enum('NHAP_LAI','HUY')", nullable: false),
                    ngoai_le = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    gia_von = table.Column<decimal>(type: "decimal(15,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ct_phieu_doi_tra", x => new { x.phieu_doi_tra_id, x.san_pham_id });
                    table.CheckConstraint("ck_ctdt_khac", "ly_do <> 'KHAC' OR ghi_chu_ly_do IS NOT NULL");
                    table.CheckConstraint("ck_ctdt_so_luong", "so_luong > 0");
                    table.ForeignKey(
                        name: "fk_ctdt_dong_hd",
                        columns: x => new { x.hoa_don_id, x.san_pham_id },
                        principalTable: "ct_hoa_don",
                        principalColumns: new[] { "hoa_don_id", "san_pham_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ctdt_phieu",
                        column: x => x.phieu_doi_tra_id,
                        principalTable: "phieu_doi_tra",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            // MySql.EntityFrameworkCore 10.0.13 omits STORED in
            // generated CREATE TABLE SQL. Add both computed columns
            // explicitly to preserve the reference MySQL schema.
            migrationBuilder.Sql("""
                ALTER TABLE `ct_phieu_doi_tra`
                ADD COLUMN `thanh_tien` BIGINT UNSIGNED
                    GENERATED ALWAYS AS (so_luong * don_gia_thuc_tra) STORED,
                ADD COLUMN `gia_tri_huy` DECIMAL(17,2)
                    GENERATED ALWAYS AS (
                        IF(cach_xu_ly = 'HUY', so_luong * gia_von, 0)
                    ) STORED
                """);

            migrationBuilder.CreateIndex(
                name: "idx_ctdt_dong_hd",
                table: "ct_phieu_doi_tra",
                columns: new[] { "hoa_don_id", "san_pham_id" });

            migrationBuilder.CreateIndex(
                name: "idx_ctdt_san_pham",
                table: "ct_phieu_doi_tra",
                column: "san_pham_id");
            // Ensure every returned item belongs to the original invoice
            // recorded by its return receipt.
            migrationBuilder.Sql("""
                ALTER TABLE `ct_phieu_doi_tra`
                ADD CONSTRAINT `fk_ctdt_phieu_hd_goc`
                FOREIGN KEY (`phieu_doi_tra_id`, `hoa_don_id`)
                REFERENCES `phieu_doi_tra` (`id`, `hoa_don_goc_id`)
                ON DELETE RESTRICT
                """);

            // BR-DT-03: Return receipt items are immutable.
            migrationBuilder.Sql("""
                CREATE TRIGGER trg_ct_doi_tra_khong_sua
                BEFORE UPDATE ON ct_phieu_doi_tra
                FOR EACH ROW
                SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'BR-DT-03: ct_phieu_doi_tra khong duoc sua'
                """);

            migrationBuilder.Sql("""
                CREATE TRIGGER trg_ct_doi_tra_khong_xoa
                BEFORE DELETE ON ct_phieu_doi_tra
                FOR EACH ROW
                SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'BR-DT-03: ct_phieu_doi_tra khong duoc xoa'
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP TRIGGER IF EXISTS trg_ct_doi_tra_khong_xoa");

            migrationBuilder.Sql(
                "DROP TRIGGER IF EXISTS trg_ct_doi_tra_khong_sua");
            migrationBuilder.DropTable(
                name: "ct_phieu_doi_tra");
        }
    }
}
