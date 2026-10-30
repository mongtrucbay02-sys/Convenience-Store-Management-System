using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConvenienceStore.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class DatabaseViewsAndStockCardTriggers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE VIEW v_gia_ban_hien_hanh AS
                SELECT san_pham_id, id AS lich_su_gia_id, gia_ban, hieu_luc_tu
                FROM (
                  SELECT l.*, ROW_NUMBER() OVER (PARTITION BY l.san_pham_id ORDER BY l.hieu_luc_tu DESC, l.id DESC) AS thu_tu
                  FROM lich_su_gia l
                  WHERE l.huy_luc IS NULL AND l.hieu_luc_tu <= NOW()
                ) x
                WHERE x.thu_tu = 1
                """);

            migrationBuilder.Sql("""
                CREATE VIEW v_chuong_trinh_khuyen_mai AS
                SELECT c.*,
                  CASE
                    WHEN c.trang_thai = 'NHAP' THEN 'NHAP'
                    WHEN c.trang_thai = 'DA_KET_THUC' OR NOW() >= c.ket_thuc_luc THEN 'DA_KET_THUC'
                    WHEN c.trang_thai = 'TAM_DUNG' THEN 'TAM_DUNG'
                    WHEN NOW() < c.bat_dau_luc THEN 'DA_LEN_LICH'
                    ELSE 'DANG_CHAY'
                  END AS trang_thai_hien_thi
                FROM chuong_trinh_khuyen_mai c
                """);

            migrationBuilder.Sql("""
                CREATE VIEW v_luot_dung_khuyen_mai AS
                SELECT d.khuyen_mai_id AS chuong_trinh_id,
                       h.id AS hoa_don_id, h.ma_hoa_don, h.thanh_toan_luc, h.thu_ngan_id, h.khach_hang_id,
                       SUM(d.so_luong) AS so_luong_san_pham,
                       SUM(d.so_luong * d.giam_don_vi) AS tien_giam
                FROM ct_hoa_don d
                JOIN hoa_don h ON h.id = d.hoa_don_id
                WHERE d.khuyen_mai_id IS NOT NULL AND h.trang_thai = 'DA_THANH_TOAN'
                GROUP BY d.khuyen_mai_id, h.id, h.ma_hoa_don, h.thanh_toan_luc, h.thu_ngan_id, h.khach_hang_id
                """);

            migrationBuilder.Sql("""
                CREATE TRIGGER trg_the_kho_khong_sua BEFORE UPDATE ON the_kho FOR EACH ROW
                  SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'BR-KHO-03: the_kho khong duoc sua'
                """);

            migrationBuilder.Sql("""
                CREATE TRIGGER trg_the_kho_khong_xoa BEFORE DELETE ON the_kho FOR EACH ROW
                  SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'BR-KHO-03: the_kho khong duoc xoa'
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_the_kho_khong_xoa");

            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_the_kho_khong_sua");

            migrationBuilder.Sql("DROP VIEW IF EXISTS v_luot_dung_khuyen_mai");

            migrationBuilder.Sql("DROP VIEW IF EXISTS v_chuong_trinh_khuyen_mai");

            migrationBuilder.Sql("DROP VIEW IF EXISTS v_gia_ban_hien_hanh");
        }
    }
}
