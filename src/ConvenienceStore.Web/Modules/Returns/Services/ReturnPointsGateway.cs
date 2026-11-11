using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Customers;
using ConvenienceStore.Web.Shared.Data;
using ConvenienceStore.Web.Shared.Errors;

namespace ConvenienceStore.Web.Modules.Returns.Services;

/// <summary>
/// Thu hồi điểm KH-06 trong cùng transaction với phiếu trả và biến động kho.
/// Không tự mở hoặc commit transaction.
/// </summary>
public interface IReturnPointsGateway
{
    Task<int> RecoverAsync(CuaHangDbContext transactionContext, long invoiceId, long returnId,
        int cashierId, long refund, CancellationToken ct);
}

/// <summary>
/// Thu hồi điểm theo tỷ lệ giá trị thực trả đã hoàn:
/// floor(điểm đã cộng * tổng tiền hoàn lũy kế / tổng thanh toán hóa đơn).
/// Tính lũy kế để nhiều lần trả từng phần không làm mất điểm do làm tròn.
/// Số điểm thu hồi không vượt số điểm đã cộng và số dư hiện tại của khách.
/// </summary>
public sealed class MySqlReturnPointsGateway : IReturnPointsGateway
{
    private sealed record InvoicePoints(uint? CustomerId, long Total, int PointsEarned);
    private sealed record ReturnTotals(long Refunded, long PointsAlreadyRecovered);

    public async Task<int> RecoverAsync(CuaHangDbContext db, long invoiceId, long returnId,
        int cashierId, long refund, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(db);
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Thu hồi điểm phải nằm trong transaction DT-02.");
        if (invoiceId <= 0 || returnId <= 0 || cashierId <= 0 || refund < 0)
            throw LoiNghiepVu.DuLieuKhongHopLe("points", "Thông tin thu hồi điểm không hợp lệ.");

        var invoice = await db.DocMotDongAsync("""
            SELECT khach_hang_id, tong_thanh_toan, diem_cong
            FROM hoa_don
            WHERE id=@id
            FOR UPDATE
            """, r => new InvoicePoints(
                r.IsDBNull(0) ? (uint?)null : Convert.ToUInt32(r[0]),
                r.SoNguyenDai(1),
                r.SoNguyen(2)),
            ct, ("@id", invoiceId));

        if (invoice is null)
            throw LoiNghiepVu.KhongTimThay("Không tìm thấy hóa đơn gốc.");
        if (invoice.CustomerId is null || invoice.PointsEarned <= 0 || refund == 0)
            return 0;
        if (invoice.Total <= 0)
            throw new LoiNghiepVu("DIEM_HOA_DON_KHONG_HOP_LE", 409,
                "Không thể tính điểm thu hồi vì tổng thanh toán hóa đơn không hợp lệ.");

        // Phiếu hiện tại đã được insert HOAN_TAT trong transaction này.
        // SUM bao gồm phiếu hiện tại và mọi phiếu trả đã hoàn tất trước đó.
        var totals = await db.DocMotDongAsync("""
            SELECT COALESCE(SUM(tien_hoan),0), COALESCE(SUM(diem_thu_hoi),0)
            FROM phieu_doi_tra
            WHERE hoa_don_goc_id=@invoice
              AND loai='TRA_HANG'
              AND trang_thai='HOAN_TAT'
            """, r => new ReturnTotals(r.SoNguyenDai(0), r.SoNguyenDai(1)),
            ct, ("@invoice", invoiceId));

        if (totals is null)
            throw new InvalidOperationException("Không đọc được tổng hợp phiếu trả.");

        var currentBalanceValue = await db.DocMotGiaTriAsync("""
            SELECT diem_hien_co
            FROM khach_hang
            WHERE id=@id AND trang_thai='HOAT_DONG'
            FOR UPDATE
            """, ct, ("@id", invoice.CustomerId.Value));

        if (currentBalanceValue is null)
            throw LoiNghiepVu.KhongTimThay("Khách hàng không còn hoạt động để thu hồi điểm.");

        long refundedCapped = Math.Min(totals.Refunded, invoice.Total);
        long targetCumulative = (long)Math.Floor(
            (decimal)invoice.PointsEarned * refundedCapped / invoice.Total);
        long remainingTarget = Math.Max(0, targetCumulative - totals.PointsAlreadyRecovered);
        long balance = Convert.ToUInt32(currentBalanceValue);
        int recover = checked((int)Math.Min(remainingTarget, balance));

        if (recover == 0)
            return 0;

        await CustomerPoints.ChangeAsync(
            db,
            invoice.CustomerId.Value,
            -recover,
            "THU_HOI_TRA_HANG",
            cashierId,
            checked((uint)invoiceId),
            checked((uint)returnId),
            $"Thu hồi điểm theo giá trị trả hàng của phiếu #{returnId}",
            ct);

        return recover;
    }
}
