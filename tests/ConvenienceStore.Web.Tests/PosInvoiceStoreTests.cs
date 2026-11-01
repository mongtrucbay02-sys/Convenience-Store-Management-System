using ConvenienceStore.Web.Services.Pos;
using ConvenienceStore.Web.Shared.Data;
using ConvenienceStore.Web.Tests.Inventory;
using Microsoft.Extensions.Logging.Abstractions;

namespace ConvenienceStore.Web.Tests;

[Collection(CsdlKhoCollection.Ten)]
public sealed class PosInvoiceStoreTests(CsdlKhoFixture csdl)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReadsSavedLinesPromotionsPaymentsPointsAndBothReturnLinks(bool cancelled)
    {
        var product = await new DuLieuTestKho(csdl).TaoSanPhamAsync("POS11");
        await using var db = csdl.TaoDbContext();
        // Mỗi test tạo dữ liệu trong giao tác và rollback; không dùng database nghiệp vụ.
        await using var transaction = await db.Database.BeginTransactionAsync();
        var promotion = Convert.ToInt32(await db.DocMotGiaTriAsync("""
            INSERT INTO chuong_trinh_khuyen_mai (ma, ten, loai_giam, gia_tri, bat_dau_luc, ket_thuc_luc, nguoi_tao_id)
            VALUES (@ma, 'Khuyến mãi test', 'SO_TIEN', 5000, NOW(), DATE_ADD(NOW(), INTERVAL 1 DAY), @ad);
            SELECT LAST_INSERT_ID();
            """, default, ("@ma", "KM" + Guid.NewGuid().ToString("N")[..10]), ("@ad", csdl.TaiKhoanAdminId)));
        var customer = Convert.ToInt32(await db.DocMotGiaTriAsync("""
            INSERT INTO khach_hang (ho_ten, so_dien_thoai, nguoi_tao_id) VALUES ('Khách POS11', @sdt, @tn);
            SELECT LAST_INSERT_ID();
            """, default, ("@sdt", "0" + Random.Shared.Next(100000000, 999999999)), ("@tn", csdl.TaiKhoanThuNganId)));
        async Task<long> CreateInvoice()
        {
            var cart = Convert.ToInt32(await db.DocMotGiaTriAsync("""
                INSERT INTO gio_hang (thu_ngan_id, trang_thai, hoan_tat_luc) VALUES (@tn, 'DA_HOAN_TAT', NOW());
                SELECT LAST_INSERT_ID();
                """, default, ("@tn", csdl.TaiKhoanThuNganId)));
            var id = Convert.ToInt64(await db.DocMotGiaTriAsync("""
                INSERT INTO hoa_don (ma_hoa_don, gio_hang_id, thu_ngan_id, khach_hang_id, thanh_toan_luc,
                    tong_tien_hang, tong_giam, tong_thanh_toan, tien_tru_hang_doi, tien_khach_dua, tien_thua, diem_cong, so_tien_mot_diem)
                VALUES (@ma, @gio, @tn, @kh, NOW(), 100000, 10000, 90000, 40000, 60000, 10000, 9, 10000);
                SELECT LAST_INSERT_ID();
                """, default, ("@ma", "HD" + Guid.NewGuid().ToString("N")[..10]), ("@gio", cart),
                ("@tn", csdl.TaiKhoanThuNganId), ("@kh", customer)));
            await db.ChayAsync("""
                INSERT INTO ct_hoa_don (hoa_don_id, san_pham_id, ten_san_pham, don_vi_tinh, so_luong, don_gia, khuyen_mai_id, giam_don_vi, gia_von)
                VALUES (@hd, @sp, 'Tên snapshot lúc bán', 'Hộp', 2, 50000, @km, 5000, 30000);
                """, default, ("@hd", id), ("@sp", product), ("@km", promotion));
            return id;
        }
        var original = await CreateInvoice();
        var replacement = await CreateInvoice();
        var policy = Convert.ToInt32(await db.DocMotGiaTriAsync("""
            INSERT INTO chinh_sach_doi_tra (hieu_luc_tu) VALUES (NOW()); SELECT LAST_INSERT_ID();
            """, default));
        await db.ChayAsync("""
            INSERT INTO phieu_doi_tra (ma_phieu, loai, hoa_don_goc_id, hoa_don_moi_id, chinh_sach_id,
                trang_thai, gia_tri_hang_tra, tien_thu_them, diem_thu_hoi, thu_ngan_id, hoan_tat_luc)
            VALUES (@ma, 'DOI_HANG', @goc, @moi, @cs, 'HOAN_TAT', 40000, 50000, 4, @tn, NOW());
            """, default, ("@ma", "DT" + Guid.NewGuid().ToString("N")[..10]), ("@goc", original),
            ("@moi", replacement), ("@cs", policy), ("@tn", csdl.TaiKhoanThuNganId));
        if (cancelled)
            await db.ChayAsync("""
                UPDATE hoa_don SET trang_thai = 'DA_HUY', nguoi_huy_id = @ad, huy_luc = NOW(), ly_do_huy = 'Test POS11'
                WHERE id = @hd
                """, default, ("@ad", csdl.TaiKhoanAdminId), ("@hd", original));
        await db.ChayAsync("UPDATE san_pham SET ten_san_pham = 'Tên hiện tại đã đổi' WHERE id = @sp", default, ("@sp", product));
        var store = new MySqlPosInvoiceStore(db, NullLogger<MySqlPosInvoiceStore>.Instance);
        var invoice = (await store.GetByIdAsync(original))!;
        Assert.Equal(cancelled, invoice.IsCancelled);
        Assert.Equal("Tên snapshot lúc bán", Assert.Single(invoice.Items).ProductName);
        Assert.Equal(50000, invoice.Items[0].UnitPrice);
        Assert.Equal(10000, invoice.Items[0].Discount);
        Assert.Equal(promotion, invoice.Items[0].PromotionId);
        Assert.Equal("Hộp", invoice.Items[0].Unit);
        Assert.Equal("Khách POS11", invoice.CustomerName);
        Assert.Equal(40000, invoice.ExchangeCredit);
        Assert.Equal(60000, invoice.CashReceived);
        Assert.Equal(10000, invoice.Change);
        Assert.Equal(9, invoice.PointsEarned);
        Assert.True(Assert.Single(invoice.RelatedReturns).IsOriginalInvoice);
        Assert.False(Assert.Single((await store.GetByIdAsync(replacement))!.RelatedReturns).IsOriginalInvoice);
        if (cancelled)
        {
            Assert.Equal("Test POS11", invoice.CancellationReason);
            Assert.NotNull(invoice.CancelledAt);
        }
        await transaction.RollbackAsync();
    }
}
