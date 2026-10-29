
using System;
using System.Threading;
using System.Threading.Tasks;
using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Models.Pos;
using ConvenienceStore.Web.Shared.Data;
using Microsoft.Extensions.Logging;

namespace ConvenienceStore.Web.Services.Pos;

/// <summary>
/// POS-07: Đọc hóa đơn đã thanh toán để hiển thị
/// và in phiếu tính tiền K80.
///
/// Sử dụng dữ liệu đã lưu tại thời điểm bán,
/// không lấy lại giá bán từ bảng san_pham.
/// </summary>
public sealed class MySqlPosInvoiceStore(
    CuaHangDbContext db,
    ILogger<MySqlPosInvoiceStore> log
) : IPosInvoiceStore
{
    public async Task<PosInvoiceViewModel?> GetByIdAsync(
        long invoiceId,
        CancellationToken cancellationToken = default)
    {
        // 1. Kiểm tra ID hóa đơn
        if (invoiceId <= 0)
        {
            return null;
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            // 2. Đọc thông tin hóa đơn
            var invoice = await db.DocMotDongAsync(
                """
                SELECT
                    h.id,
                    h.ma_hoa_don,
                    h.thanh_toan_luc,
                    tn.ho_ten,
                    kh.ho_ten,
                    h.trang_thai,
                    h.tong_tien_hang,
                    h.tong_giam,
                    h.tong_thanh_toan,
                    h.tien_khach_dua,
                    h.tien_thua,
                    h.diem_cong,
                    h.thu_ngan_id,
                    h.khach_hang_id, kh.so_dien_thoai, h.tien_tru_hang_doi,
                    h.huy_luc, h.ly_do_huy, nh.ho_ten
                FROM hoa_don h
                INNER JOIN tai_khoan tn
                    ON tn.id = h.thu_ngan_id
                LEFT JOIN khach_hang kh
                    ON kh.id = h.khach_hang_id
                LEFT JOIN tai_khoan nh ON nh.id = h.nguoi_huy_id
                WHERE h.id = @id
                  AND h.thanh_toan_luc IS NOT NULL
                LIMIT 1
                """,
                d => new PosInvoiceViewModel
                {
                    InvoiceId = d.SoNguyenDai(0),
                    InvoiceCode = d.Chuoi(1),
                    CreatedAt = d.NgayGio(2),
                    CashierName = d.Chuoi(3),
                    CustomerName = d.ChuoiHoacNull(4),
                    Status = d.Chuoi(5),
                    Subtotal = d.SoNguyenDai(6),
                    Discount = d.SoNguyenDai(7),
                    Total = d.SoNguyenDai(8),
                    CashReceived = d.SoNguyenDai(9),
                    Change = d.SoNguyenDai(10),
                    PointsEarned = d.SoNguyen(11),
                    CashierId = d.SoNguyen(12),
                    CustomerId = d.IsDBNull(13) ? null : d.SoNguyen(13),
                    CustomerPhone = d.ChuoiHoacNull(14),
                    ExchangeCredit = d.SoNguyenDai(15),
                    CancelledAt = d.NgayGioHoacNull(16),
                    CancellationReason = d.ChuoiHoacNull(17),
                    CancelledBy = d.ChuoiHoacNull(18)
                },
                cancellationToken,
                ("@id", invoiceId)
            );

            // Không tìm thấy hóa đơn đã thanh toán
            if (invoice is null)
            {
                log.LogWarning(
                    "POS-07: Không tìm thấy hóa đơn đã " +
                    "thanh toán với ID {InvoiceId}",
                    invoiceId);

                return null;
            }

            // 3. Đọc chi tiết hóa đơn
            // Sử dụng tên và giá tại thời điểm bán.
            var items = await db.DocDanhSachAsync(
                """
                SELECT
                    d.ten_san_pham, d.so_luong, d.don_gia,
                    d.so_luong * d.giam_don_vi, d.thanh_tien,
                    d.san_pham_id, d.don_vi_tinh, d.khuyen_mai_id, km.ma
                FROM ct_hoa_don d
                LEFT JOIN chuong_trinh_khuyen_mai km ON km.id = d.khuyen_mai_id
                WHERE d.hoa_don_id = @id
                ORDER BY d.san_pham_id
                """,
                d => new PosInvoiceItemViewModel
                {
                    ProductName = d.Chuoi(0),
                    Quantity = d.SoNguyen(1),
                    UnitPrice = d.SoNguyenDai(2),
                    Discount = d.SoNguyenDai(3),
                    LineTotal = d.SoNguyenDai(4),
                    ProductId = d.SoNguyen(5),
                    Unit = d.Chuoi(6),
                    PromotionId = d.IsDBNull(7) ? null : d.SoNguyen(7),
                    PromotionCode = d.ChuoiHoacNull(8)
                },
                cancellationToken,
                ("@id", invoiceId)
            );

            // 4. Kiểm tra chi tiết hóa đơn
            if (items.Count == 0)
            {
                log.LogError(
                    "POS-07: Hóa đơn {InvoiceId} " +
                    "không có chi tiết sản phẩm.",
                    invoiceId);

                throw new InvalidOperationException(
                    "Hóa đơn không có chi tiết sản phẩm. " +
                    "Vui lòng liên hệ quản trị viên.");
            }

            // 5. Đưa danh sách sản phẩm vào ViewModel
            invoice.Items.AddRange(items);

            // Liên kết theo cả hóa đơn gốc và hóa đơn mới của phiếu đổi hàng.
            invoice.RelatedReturns = await db.DocDanhSachAsync(
                """
                SELECT id, ma_phieu, loai, trang_thai, ngay_lap, tien_hoan,
                       tien_thu_them, diem_thu_hoi, hoa_don_goc_id
                FROM phieu_doi_tra
                WHERE hoa_don_goc_id = @id OR hoa_don_moi_id = @id
                ORDER BY ngay_lap DESC, id DESC
                """,
                d => new PosInvoiceReturn(d.SoNguyenDai(0), d.Chuoi(1), d.Chuoi(2),
                    d.Chuoi(3), d.NgayGio(4), d.SoNguyenDai(5), d.SoNguyenDai(6),
                    d.SoNguyen(7), d.SoNguyenDai(8) == invoiceId),
                cancellationToken, ("@id", invoiceId));

            log.LogInformation(
                "POS-07: Đọc thành công hóa đơn " +
                "{InvoiceCode}, ID {InvoiceId}",
                invoice.InvoiceCode,
                invoiceId);

            return invoice;
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            log.LogError(
                ex,
                "POS-07: Lỗi khi đọc hóa đơn {InvoiceId}",
                invoiceId);

            throw;
        }
    }
}
