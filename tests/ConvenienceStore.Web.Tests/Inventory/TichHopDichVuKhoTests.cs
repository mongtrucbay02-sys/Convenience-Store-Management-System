using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Inventory.Models;
using ConvenienceStore.Web.Modules.Inventory.Services;
using ConvenienceStore.Web.Shared.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ConvenienceStore.Web.Tests.Inventory;

[Collection(CsdlKhoCollection.Ten)]
[Trait("Category", "DichVuKho")]
public sealed class TichHopDichVuKhoTests(CsdlKhoFixture csdl)
{
    private readonly DuLieuTestKho _duLieu = new(csdl);
    private readonly BoTroGoiDichVuKho _goi = new(csdl);

    [Fact]
    public async Task Nh06_XacNhanNhapKho_GhiTheKhoTheoMaPhieu_ChuyenDaNhapKho()
    {
        var sp1 = await _goi.TaoSanPhamCoTonAsync("NH06", 10, 8000m);
        var sp2 = await _goi.TaoSanPhamCoTonAsync("NH06", 0, 0m);
        var (phieuId, maPhieu) = await TaoPhieuNhapNhapAsync((sp1, 20, 9500), (sp2, 5, 12000));

        await using (var db = csdl.TaoDbContext())
        {
            await using var giaoTac = await db.Database.BeginTransactionAsync();
            var trangThai = Convert.ToString(await db.DocMotGiaTriAsync(
                "SELECT trang_thai FROM phieu_nhap WHERE id = @id FOR UPDATE", default, ("@id", phieuId)));
            Assert.Equal("NHAP", trangThai);

            var dong = await db.DocDanhSachAsync(
                "SELECT san_pham_id, so_luong, gia_nhap FROM ct_phieu_nhap WHERE phieu_nhap_id = @id ORDER BY san_pham_id",
                doc => new DongBienDong(doc.SoNguyen(0), SoLuong: doc.SoNguyen(1), GiaVon: doc.SoThapPhan(2)),
                default, ("@id", phieuId));

            var kq = await BoTroGoiDichVuKho.TaoDichVu(db).GhiNhanBienDongAsync(new YeuCauBienDong(
                LoaiBienDong.NHAP_HANG, phieuId, maPhieu, csdl.TaiKhoanKhoId, dong));

            var soDong = await db.ChayAsync(
                """
                UPDATE phieu_nhap
                SET trang_thai = 'DA_NHAP_KHO', nguoi_xac_nhan_id = @nguoi, xac_nhan_luc = NOW(), phien_ban = phien_ban + 1
                WHERE id = @id AND trang_thai = 'NHAP'
                """,
                default, ("@nguoi", csdl.TaiKhoanKhoId), ("@id", phieuId));
            Assert.Equal(1, soDong);
            Assert.Equal(9000m, kq.Dong.Single(d => d.SanPhamId == sp1).GiaVonSau);
            await giaoTac.CommitAsync();
        }

        Assert.Equal(30, (await _duLieu.DocTonKhoAsync(sp1))!.SoLuongTon);
        Assert.Equal(new TonKhoDoc(5, 12000m, 5), await _duLieu.DocTonKhoAsync(sp2));
        Assert.Equal(2L, Convert.ToInt64(await csdl.DocMotGiaTriAsync(
            "SELECT COUNT(*) FROM the_kho WHERE loai_bien_dong = 'NHAP_HANG' AND ma_chung_tu = @ma AND chung_tu_id = @id",
            ("@ma", maPhieu), ("@id", phieuId))));
        Assert.Equal("DA_NHAP_KHO", await csdl.DocMotGiaTriAsync(
            "SELECT trang_thai FROM phieu_nhap WHERE id = @id", ("@id", phieuId)));
    }

    [Fact]
    public async Task Pos07_HoanTatDon_LuuGiaVonApDungVaoDongHoaDon()
    {
        var sp = await _goi.TaoSanPhamCoTonAsync("POS07", 10, 8000m);
        var maHoaDon = DuLieuTestKho.MaChungTuGia("HD");

        await using (var db = csdl.TaoDbContext())
        {
            await using var giaoTac = await db.Database.BeginTransactionAsync();
            var (gioId, hoaDonId) = await ThemGioVaHoaDonAsync(db, maHoaDon, 3 * 15000);

            var kq = await BoTroGoiDichVuKho.TaoDichVu(db).GhiNhanBienDongAsync(new YeuCauBienDong(
                LoaiBienDong.BAN_HANG, hoaDonId, maHoaDon, csdl.TaiKhoanThuNganId,
                [new DongBienDong(sp, SoLuong: 3)]));

            await ThemDongHoaDonAsync(db, hoaDonId, sp, 3, 15000, kq.Dong[0].GiaVonApDung);
            await db.ChayAsync(
                "UPDATE gio_hang SET trang_thai = 'DA_HOAN_TAT', hoan_tat_luc = NOW() WHERE id = @id",
                default, ("@id", gioId));
            await giaoTac.CommitAsync();
        }

        Assert.Equal(7, (await _duLieu.DocTonKhoAsync(sp))!.SoLuongTon);
        Assert.Equal(8000m, Convert.ToDecimal(await csdl.DocMotGiaTriAsync(
            "SELECT d.gia_von FROM ct_hoa_don d JOIN hoa_don h ON h.id = d.hoa_don_id WHERE h.ma_hoa_don = @ma",
            ("@ma", maHoaDon))));
        Assert.Equal(1L, Convert.ToInt64(await csdl.DocMotGiaTriAsync(
            "SELECT COUNT(*) FROM the_kho WHERE loai_bien_dong = 'BAN_HANG' AND ma_chung_tu = @ma", ("@ma", maHoaDon))));
    }

    [Fact]
    public async Task Pos07_TonKhongDu_Rollback_KhongConHoaDonVaTheKho()
    {
        var sp = await _goi.TaoSanPhamCoTonAsync("POS07E1", 2, 8000m);
        var maHoaDon = DuLieuTestKho.MaChungTuGia("HD");
        LoiDichVuKho? loi = null;

        await using (var db = csdl.TaoDbContext())
        {
            await using var giaoTac = await db.Database.BeginTransactionAsync();
            var (_, hoaDonId) = await ThemGioVaHoaDonAsync(db, maHoaDon, 5 * 15000);
            try
            {
                await BoTroGoiDichVuKho.TaoDichVu(db).GhiNhanBienDongAsync(new YeuCauBienDong(
                    LoaiBienDong.BAN_HANG, hoaDonId, maHoaDon, csdl.TaiKhoanThuNganId,
                    [new DongBienDong(sp, SoLuong: 5)]));
                await giaoTac.CommitAsync();
            }
            catch (LoiDichVuKho e)
            {
                loi = e;
                await giaoTac.RollbackAsync();
            }
        }

        Assert.NotNull(loi);
        Assert.Equal(MaLoiKho.TonKhongDu, loi!.Ma);
        Assert.Contains("chỉ còn 2 sản phẩm", loi.Message);
        Assert.Equal(0L, Convert.ToInt64(await csdl.DocMotGiaTriAsync(
            "SELECT COUNT(*) FROM hoa_don WHERE ma_hoa_don = @ma", ("@ma", maHoaDon))));
        Assert.Equal(2, (await _duLieu.DocTonKhoAsync(sp))!.SoLuongTon);
    }

    [Fact]
    public async Task Pos07_LoiSauKhiTruTon_Rollback_KhongCoHoaDon_TonKhongDoi_NfrTv02()
    {
        var sp = await _goi.TaoSanPhamCoTonAsync("TV02", 10, 8000m);
        var maHoaDon = DuLieuTestKho.MaChungTuGia("HD");

        await using (var db = csdl.TaoDbContext())
        {
            await using var giaoTac = await db.Database.BeginTransactionAsync();
            var (_, hoaDonId) = await ThemGioVaHoaDonAsync(db, maHoaDon, 3 * 15000);
            var kq = await BoTroGoiDichVuKho.TaoDichVu(db).GhiNhanBienDongAsync(new YeuCauBienDong(
                LoaiBienDong.BAN_HANG, hoaDonId, maHoaDon, csdl.TaiKhoanThuNganId,
                [new DongBienDong(sp, SoLuong: 3)]));
            Assert.Equal(7, kq.Dong[0].TonSau);

            await Assert.ThrowsAnyAsync<Exception>(() => ThemDongHoaDonAsync(db, hoaDonId, sp, 0, 15000, kq.Dong[0].GiaVonApDung));
            await giaoTac.RollbackAsync();
        }

        Assert.Equal(10, (await _duLieu.DocTonKhoAsync(sp))!.SoLuongTon);
        Assert.Equal(0, await _duLieu.DemTheKhoAsync(sp, "BAN_HANG"));
        Assert.Equal(0L, Convert.ToInt64(await csdl.DocMotGiaTriAsync(
            "SELECT COUNT(*) FROM hoa_don WHERE ma_hoa_don = @ma", ("@ma", maHoaDon))));
    }

    private async Task<(int Id, string Ma)> TaoPhieuNhapNhapAsync(params (int SanPhamId, int SoLuong, int GiaNhap)[] dong)
    {
        var maNcc = "NCC" + Guid.NewGuid().ToString("N")[..8];
        var sdt = "09" + Random.Shared.Next(0, 100_000_000).ToString("D8");
        var maPhieu = DuLieuTestKho.MaChungTuGia("PN");
        await csdl.ChayAsync(
            "INSERT INTO nha_cung_cap (ma, ten, so_dien_thoai) VALUES (@ma, @ten, @sdt)",
            ("@ma", maNcc), ("@ten", "Nhà cung cấp " + maNcc), ("@sdt", sdt));
        await csdl.ChayAsync(
            """
            INSERT INTO phieu_nhap (ma_phieu, nha_cung_cap_id, ngay_nhap, nguoi_lap_id)
            VALUES (@ma, (SELECT id FROM nha_cung_cap WHERE ma = @ncc), CURDATE(), @nguoi)
            """,
            ("@ma", maPhieu), ("@ncc", maNcc), ("@nguoi", csdl.TaiKhoanKhoId));
        var phieuId = Convert.ToInt32(await csdl.DocMotGiaTriAsync(
            "SELECT id FROM phieu_nhap WHERE ma_phieu = @ma", ("@ma", maPhieu)));
        foreach (var (sanPhamId, soLuong, giaNhap) in dong)
            await csdl.ChayAsync(
                """
                INSERT INTO ct_phieu_nhap (phieu_nhap_id, san_pham_id, ten_san_pham, don_vi_tinh, so_luong, gia_nhap)
                SELECT @phieu, id, ten_san_pham, don_vi_tinh, @soLuong, @gia FROM san_pham WHERE id = @sp
                """,
                ("@phieu", phieuId), ("@soLuong", soLuong), ("@gia", giaNhap), ("@sp", sanPhamId));
        return (phieuId, maPhieu);
    }

    private async Task<(int GioId, int HoaDonId)> ThemGioVaHoaDonAsync(CuaHangDbContext db, string maHoaDon, int tongTien)
    {
        await db.ChayAsync(
            "UPDATE gio_hang SET trang_thai = 'DA_HUY', huy_luc = NOW(), ly_do_huy = 'THU_NGAN_HUY', gia_tri_khi_huy = 0 " +
            "WHERE thu_ngan_id = @tn AND trang_thai = 'DANG_BAN'",
            default, ("@tn", csdl.TaiKhoanThuNganId));
        var gioId = Convert.ToInt32(await db.DocMotGiaTriAsync(
            "INSERT INTO gio_hang (thu_ngan_id) VALUES (@tn); SELECT LAST_INSERT_ID();",
            default, ("@tn", csdl.TaiKhoanThuNganId)));
        var hoaDonId = Convert.ToInt32(await db.DocMotGiaTriAsync(
            """
            INSERT INTO hoa_don (ma_hoa_don, gio_hang_id, thu_ngan_id, thanh_toan_luc, tong_tien_hang, tong_thanh_toan,
                                 tien_khach_dua, tien_thua)
            VALUES (@ma, @gio, @tn, NOW(), @tong, @tong, @tong, 0);
            SELECT LAST_INSERT_ID();
            """,
            default, ("@ma", maHoaDon), ("@gio", gioId), ("@tn", csdl.TaiKhoanThuNganId), ("@tong", tongTien)));
        return (gioId, hoaDonId);
    }

    private static Task<int> ThemDongHoaDonAsync(
        CuaHangDbContext db, int hoaDonId, int sanPhamId, int soLuong, int donGia, decimal giaVon) =>
        db.ChayAsync(
            """
            INSERT INTO ct_hoa_don (hoa_don_id, san_pham_id, ten_san_pham, don_vi_tinh, so_luong, don_gia, gia_von)
            SELECT @hd, id, ten_san_pham, don_vi_tinh, @soLuong, @donGia, @giaVon FROM san_pham WHERE id = @sp
            """,
            default, ("@hd", hoaDonId), ("@soLuong", soLuong), ("@donGia", donGia), ("@giaVon", giaVon), ("@sp", sanPhamId));
}
