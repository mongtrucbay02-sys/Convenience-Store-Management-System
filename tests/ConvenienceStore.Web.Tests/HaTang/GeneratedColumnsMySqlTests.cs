using ConvenienceStore.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Tests.HaTang;

public sealed class GeneratedColumnsMySqlTests(CsdlMySqlTam csdl) : IClassFixture<CsdlMySqlTam>
{
    private const uint FixtureId = 910001;

    [Fact]
    public async Task InsertAndUpdate_MutableRowsAndInsertImmutableRows_AreCalculatedByMySql()
    {
        await SeedParentsAsync();

        await using var db = csdl.TaoDbContext();
        var purchaseLine = new CtPhieuNhap
        {
            PhieuNhapId = FixtureId,
            SanPhamId = FixtureId,
            TenSanPham = "Generated purchase line",
            DonViTinh = "cai",
            SoLuong = 2,
            GiaNhap = 300
        };
        db.CtPhieuNhaps.Add(purchaseLine);
        await db.SaveChangesAsync();
        await db.Entry(purchaseLine).ReloadAsync();
        Assert.Equal(600UL, purchaseLine.ThanhTien);
        purchaseLine.SoLuong = 3;
        await db.SaveChangesAsync();
        await db.Entry(purchaseLine).ReloadAsync();
        Assert.Equal(900UL, purchaseLine.ThanhTien);

        var cart = new GioHang { ThuNganId = FixtureId, TrangThai = "DANG_BAN" };
        db.GioHangs.Add(cart);
        await db.SaveChangesAsync();
        await db.Entry(cart).ReloadAsync();
        Assert.Equal(FixtureId, cart.DangBanCuaThuNgan);
        cart.TrangThai = "TAM_GIU";
        cart.TamGiuLuc = DateTime.Now;
        await db.SaveChangesAsync();
        await db.Entry(cart).ReloadAsync();
        Assert.Null(cart.DangBanCuaThuNgan);

        await csdl.ChayAsync("""
            INSERT INTO hoa_don
              (id, ma_hoa_don, gio_hang_id, thu_ngan_id, thanh_toan_luc,
               tong_tien_hang, tong_giam, tong_thanh_toan, tien_khach_dua, tien_thua)
            VALUES (@id, 'HD-GENERATED-910001', @cart, @cashier, NOW(), 1000, 0, 1000, 1000, 0)
            """,
            ("@id", FixtureId), ("@cart", cart.Id), ("@cashier", FixtureId));

        var invoiceLine = new CtHoaDon
        {
            HoaDonId = FixtureId,
            SanPhamId = FixtureId,
            TenSanPham = "Generated invoice line",
            DonViTinh = "cai",
            SoLuong = 2,
            DonGia = 500,
            GiamDonVi = 0,
            GiaVon = 200
        };
        db.CtHoaDons.Add(invoiceLine);
        await db.SaveChangesAsync();
        await db.Entry(invoiceLine).ReloadAsync();
        Assert.Equal(1000UL, invoiceLine.ThanhTien);

        await csdl.ChayAsync("""
            INSERT INTO chinh_sach_doi_tra (id, hieu_luc_tu) VALUES (@id, NOW())
            """, ("@id", FixtureId));
        await csdl.ChayAsync("""
            INSERT INTO phieu_doi_tra
              (id, ma_phieu, loai, hoa_don_goc_id, chinh_sach_id, trang_thai,
               gia_tri_hang_tra, tien_hoan, thu_ngan_id, hoan_tat_luc)
            VALUES (@id, 'TH-GENERATED-910001', 'TRA_HANG', @invoice, @policy, 'HOAN_TAT', 800, 800, @cashier, NOW())
            """,
            ("@id", FixtureId), ("@invoice", FixtureId), ("@policy", FixtureId), ("@cashier", FixtureId));

        var returnLine = new CtPhieuDoiTra
        {
            PhieuDoiTraId = FixtureId,
            SanPhamId = FixtureId,
            HoaDonId = FixtureId,
            SoLuong = 2,
            DonGiaThucTra = 400,
            LyDo = "LOI_SAN_PHAM",
            CachXuLy = "HUY",
            GiaVon = 120
        };
        db.CtPhieuDoiTras.Add(returnLine);
        await db.SaveChangesAsync();
        await db.Entry(returnLine).ReloadAsync();
        Assert.Equal(800UL, returnLine.ThanhTien);
        Assert.Equal(240m, returnLine.GiaTriHuy);
    }

    private async Task SeedParentsAsync()
    {
        await csdl.ChayAsync("""
            INSERT INTO tai_khoan (id, ten_dang_nhap, mat_khau_bam, ho_ten, vai_tro_id)
            VALUES (@id, 'generatedcolumn910001', 'test-hash', 'Generated column test', 2)
            """, ("@id", FixtureId));
        await csdl.ChayAsync("""
            INSERT INTO danh_muc (id, ten) VALUES (@id, 'Generated column test category')
            """, ("@id", FixtureId));
        await csdl.ChayAsync("""
            INSERT INTO san_pham (id, ma, ten_san_pham, danh_muc_id, don_vi_tinh, nguoi_tao_id)
            VALUES (@id, 'GEN-910001', 'Generated column test product', @category, 'cai', @creator)
            """, ("@id", FixtureId), ("@category", FixtureId), ("@creator", FixtureId));
        await csdl.ChayAsync("""
            INSERT INTO nha_cung_cap (id, ma, ten, so_dien_thoai)
            VALUES (@id, 'GEN-NCC-910001', 'Generated column test supplier', '0900000001')
            """, ("@id", FixtureId));
        await csdl.ChayAsync("""
            INSERT INTO phieu_nhap (id, ma_phieu, nha_cung_cap_id, ngay_nhap, nguoi_lap_id)
            VALUES (@id, 'GEN-PN-910001', @supplier, CURRENT_DATE, @creator)
            """, ("@id", FixtureId), ("@supplier", FixtureId), ("@creator", FixtureId));
    }
}
