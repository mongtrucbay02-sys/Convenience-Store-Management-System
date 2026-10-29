
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
                    h.thu_ngan_id
                FROM hoa_don h
                INNER JOIN tai_khoan tn
                    ON tn.id = h.thu_ngan_id
                LEFT JOIN khach_hang kh
                    ON kh.id = h.khach_hang_id
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
                    CashierId = d.SoNguyen(12)
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
                    ten_san_pham,
                    so_luong,
                    don_gia,
                    so_luong * giam_don_vi,
                    thanh_tien
                FROM ct_hoa_don
                WHERE hoa_don_id = @id
                ORDER BY san_pham_id
                """,
                d => new PosInvoiceItemViewModel
                {
                    ProductName = d.Chuoi(0),
                    Quantity = d.SoNguyen(1),
                    UnitPrice = d.SoNguyenDai(2),
                    Discount = d.SoNguyenDai(3),
                    LineTotal = d.SoNguyenDai(4)
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
