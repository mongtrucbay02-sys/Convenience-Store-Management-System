
using System.Data;
using System.Security.Claims;
using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Customers;
using ConvenienceStore.Web.Modules.Inventory.Models;
using ConvenienceStore.Web.Modules.Inventory.Services;
using ConvenienceStore.Web.Shared.Data;
using ConvenienceStore.Web.Shared.Api;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Services.Pos;

/// <summary>
/// POS-12: Hủy hóa đơn đã thanh toán.
/// Không xóa hóa đơn. Các thay đổi nghiệp vụ phải cùng transaction.
/// </summary>
public sealed class PosInvoiceCancellationService(
    CuaHangDbContext db,
    IDichVuKho stock,
    TimeProvider time)
{
    private sealed record InvoiceRow(
        uint Id,
        string Code,
        string Status,
        uint? CustomerId,
        uint PointsEarned,
        ulong Total,
        ulong CashReceived,
        ulong Change);

    private sealed record InvoiceLine(
        int ProductId,
        int Quantity,
        decimal Cost);

    public sealed record CancellationResult(
        uint InvoiceId,
        string InvoiceCode,
        ulong CashRefund,
        uint PointsEarned,
        uint PointsReversed,
        uint PointsNotReversed,
        DateTime CancelledAt,
        string Reason);

    public async Task<CancellationResult> CancelAsync(
        long invoiceId,
        string? reason,
        ClaimsPrincipal user,
        CancellationToken ct = default)
    {
        // 1. Validate dữ liệu đầu vào.
        if (invoiceId <= 0 || invoiceId > uint.MaxValue)
        {
            throw LoiNghiepVu.DuLieuKhongHopLe(
                "invoiceId",
                "Mã hóa đơn không hợp lệ.");
        }

        reason = reason?.Trim();

        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 255)
        {
            throw LoiNghiepVu.DuLieuKhongHopLe(
                "reason",
                "Lý do hủy bắt buộc, tối đa 255 ký tự.");
        }

        int actor = GetActorId(user);

        // 2. Bắt đầu transaction.
        await using var tx =
            await db.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                ct);

        // 3. Kiểm tra tài khoản và khóa dòng tài khoản.
        // Ngăn tài khoản bị khóa hoặc đổi vai trò trong lúc hủy.
        var role = await db.DocMotGiaTriAsync("""
            SELECT v.ma
            FROM tai_khoan t
            INNER JOIN vai_tro v ON v.id = t.vai_tro_id
            WHERE t.id = @actor
              AND t.trang_thai = 'HOAT_DONG'
              AND t.bat_buoc_doi_mk = FALSE
            FOR UPDATE
            """,
            ct,
            ("@actor", actor));

        if (!string.Equals(
                Convert.ToString(role),
                "AD",
                StringComparison.Ordinal))
        {
            throw new LoiNghiepVu(
                "KHONG_DU_QUYEN",
                403,
                "Chỉ Admin/Quản lý được hủy hóa đơn đã thanh toán.");
        }

        // 4. Khóa hóa đơn để tránh hai Admin hủy đồng thời.
        var invoice = await db.DocMotDongAsync("""
            SELECT
                id,
                ma_hoa_don,
                trang_thai,
                khach_hang_id,
                diem_cong,
                tong_thanh_toan,
                tien_khach_dua,
                tien_thua
            FROM hoa_don
            WHERE id = @id
            FOR UPDATE
            """,
            r => new InvoiceRow(
                Convert.ToUInt32(r[0]),
                r.GetString(1),
                r.GetString(2),
                r.IsDBNull(3)
                    ? null
                    : Convert.ToUInt32(r[3]),
                Convert.ToUInt32(r[4]),
                Convert.ToUInt64(r[5]),
                Convert.ToUInt64(r[6]),
                Convert.ToUInt64(r[7])),
            ct,
            ("@id", invoiceId));

        if (invoice is null)
        {
            throw LoiNghiepVu.KhongTimThay(
                "Không tìm thấy hóa đơn.");
        }

        if (invoice.Status == "DA_HUY")
        {
            throw LoiNghiepVu.XungDot(
                MaLoiChung.XungDotDuLieu,
                "Hóa đơn đã được hủy trước đó.");
        }

        if (invoice.Status != "DA_THANH_TOAN")
        {
            throw LoiNghiepVu.XungDot(
                MaLoiChung.XungDotDuLieu,
                "Chỉ được hủy hóa đơn đã thanh toán.");
        }

        // 5. E1: Không cho hủy hóa đơn đã phát sinh đổi trả.
        var hasReturn = Convert.ToInt32(
            await db.DocMotGiaTriAsync("""
                SELECT COUNT(*)
                FROM phieu_doi_tra
                WHERE hoa_don_goc_id = @id
                   OR hoa_don_moi_id = @id
                """,
                ct,
                ("@id", invoiceId))) > 0;

        if (hasReturn)
        {
            throw LoiNghiepVu.XungDot(
                MaLoiChung.XungDotDuLieu,
                "Hóa đơn đã phát sinh phiếu đổi trả. " +
                "Không thể hủy; hãy tiếp tục xử lý tại chức năng đổi trả.");
        }

        // 6. Đọc chi tiết hóa đơn.
        // Tính giá vốn bình quân có trọng số nếu sản phẩm xuất hiện
        // ở nhiều dòng với giá vốn khác nhau.
        var lines = await db.DocDanhSachAsync("""
            SELECT
                san_pham_id,
                SUM(so_luong),
                CASE
                    WHEN SUM(so_luong) > 0
                    THEN SUM(so_luong * gia_von) / SUM(so_luong)
                    ELSE 0
                END
            FROM ct_hoa_don
            WHERE hoa_don_id = @id
            GROUP BY san_pham_id
            ORDER BY san_pham_id
            """,
            r => new InvoiceLine(
                Convert.ToInt32(r[0]),
                Convert.ToInt32(r[1]),
                Convert.ToDecimal(r[2])),
            ct,
            ("@id", invoiceId));

        if (lines.Count == 0)
        {
            throw LoiNghiepVu.XungDot(
                MaLoiChung.XungDotDuLieu,
                "Hóa đơn không có dòng sản phẩm để hoàn tồn.");
        }

        if (lines.Any(x =>
                x.ProductId <= 0 ||
                x.Quantity <= 0 ||
                x.Cost < 0))
        {
            throw LoiNghiepVu.XungDot(
                MaLoiChung.XungDotDuLieu,
                "Chi tiết hóa đơn có dữ liệu số lượng hoặc giá vốn không hợp lệ.");
        }

        // 7. Kiểm tra dữ liệu thanh toán.
        if (invoice.CashReceived < invoice.Change)
        {
            throw LoiNghiepVu.XungDot(
                MaLoiChung.XungDotDuLieu,
                "Dữ liệu tiền khách đưa và tiền thừa không hợp lệ.");
        }

        ulong cashRefund =
            invoice.CashReceived - invoice.Change;

        // 8. Hoàn lại tồn kho và ghi biến động HUY_HOA_DON.
        // IDichVuKho phải tham gia transaction hiện tại của DbContext.
        await stock.GhiNhanBienDongAsync(
            new YeuCauBienDong(
                LoaiBienDong.HUY_HOA_DON,
                checked((int)invoice.Id),
                invoice.Code,
                actor,
                lines.Select(x =>
                    new DongBienDong(
                        x.ProductId,
                        SoLuong: x.Quantity,
                        GiaVon: x.Cost))
                    .ToList()),
            ct);

        // 9. E2: Thu hồi điểm đã tích.
        uint pointsReversed = 0;
        uint pointsNotReversed = 0;

        if (invoice.CustomerId.HasValue &&
            invoice.PointsEarned > 0)
        {
            var current = await db.DocMotGiaTriAsync("""
                SELECT diem_hien_co
                FROM khach_hang
                WHERE id = @customer
                  AND trang_thai = 'HOAT_DONG'
                FOR UPDATE
                """,
                ct,
                ("@customer", invoice.CustomerId.Value));

            if (current is null)
            {
                throw LoiNghiepVu.XungDot(
                    MaLoiChung.XungDotDuLieu,
                    "Khách hàng gắn với hóa đơn không còn hoạt động; " +
                    "không thể thu hồi điểm an toàn.");
            }

            uint balance = Convert.ToUInt32(current);

            pointsReversed = Math.Min(
                invoice.PointsEarned,
                balance);

            pointsNotReversed =
                invoice.PointsEarned - pointsReversed;

            string pointsNote =
                pointsNotReversed == 0
                    ? $"Thu hồi điểm do hủy hóa đơn {invoice.Code}."
                    : $"Hủy hóa đơn {invoice.Code}; " +
                      $"thu hồi {pointsReversed}/{invoice.PointsEarned} điểm. " +
                      $"Còn thiếu {pointsNotReversed} điểm do khách đã sử dụng hoặc điều chỉnh.";

            // Chỉ thay đổi số dư khi thực sự có điểm cần thu hồi.
            if (pointsReversed > 0)
            {
                await CustomerPoints.ChangeAsync(
                    db,
                    invoice.CustomerId.Value,
                    -checked((int)pointsReversed),
                    "THU_HOI_HUY_HOA_DON",
                    actor,
                    invoice.Id,
                    null,
                    pointsNote,
                    ct);
            }
        }

        // 10. Giải phóng lượt sử dụng khuyến mãi.
        // Điều kiện: View v_luot_dung_khuyen_mai chỉ đếm hóa đơn
        // có trạng thái DA_THANH_TOAN.
        // Khi chuyển trạng thái sang DA_HUY, lượt sử dụng được giải phóng.

        // 11. Ghi nhận hoàn tiền mặt và lịch sử xử lý.
        // Unique(hoa_don_id) nên được thiết lập trong CSDL
        // để chống ghi nhận hoàn tiền hai lần.
        await db.ChayAsync("""
            INSERT INTO hoan_tien_hoa_don
            (
                hoa_don_id,
                so_tien_hoan,
                phuong_thuc,
                diem_da_thu_hoi,
                diem_chua_thu_hoi,
                ly_do,
                nguoi_thuc_hien_id,
                thoi_diem
            )
            VALUES
            (
                @invoice,
                @amount,
                'TIEN_MAT',
                @pointsBack,
                @pointsShort,
                @reason,
                @actor,
                NOW()
            )
            """,
            ct,
            ("@invoice", invoice.Id),
            ("@amount", cashRefund),
            ("@pointsBack", pointsReversed),
            ("@pointsShort", pointsNotReversed),
            ("@reason", reason),
            ("@actor", actor));

        // 12. Chuyển trạng thái hóa đơn.
        // Không DELETE hóa đơn, không thay đổi mã hóa đơn.
        var changed = await db.ChayAsync("""
            UPDATE hoa_don
            SET
                trang_thai = 'DA_HUY',
                nguoi_huy_id = @actor,
                huy_luc = NOW(),
                ly_do_huy = @reason
            WHERE id = @id
              AND trang_thai = 'DA_THANH_TOAN'
            """,
            ct,
            ("@actor", actor),
            ("@reason", reason),
            ("@id", invoice.Id));

        if (changed != 1)
        {
            throw LoiNghiepVu.XungDot(
                MaLoiChung.XungDotDuLieu,
                "Hóa đơn đã thay đổi bởi một thao tác khác. Hãy tải lại trang.");
        }

        // 13. Chỉ commit khi tất cả nghiệp vụ thành công.
        await tx.CommitAsync(ct);

        return new CancellationResult(
            invoice.Id,
            invoice.Code,
            cashRefund,
            invoice.PointsEarned,
            pointsReversed,
            pointsNotReversed,
            time.GetUtcNow()
                .ToOffset(GioVietNam.DoLech)
                .DateTime,
            reason);
    }

    private static int GetActorId(ClaimsPrincipal user)
    {
        var raw =
            user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub");

        if (!int.TryParse(raw, out int id) || id <= 0)
        {
            throw new LoiNghiepVu(
                "KHONG_DU_QUYEN",
                403,
                "Không xác định được tài khoản Admin.");
        }

        return id;
    }
}
