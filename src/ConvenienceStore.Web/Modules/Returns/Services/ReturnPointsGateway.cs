using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Customers.Models;
using ConvenienceStore.Web.Modules.Customers.Services;
using ConvenienceStore.Web.Shared.Data;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Modules.Returns.Services;

public interface IReturnPointsGateway
{
    Task<int> RecoverAsync(CuaHangDbContext transactionContext, long invoiceId,
        long returnId, int cashierId, long refund, CancellationToken ct);
}

public sealed class ReturnPointsGateway : IReturnPointsGateway
{
    public async Task<int> RecoverAsync(CuaHangDbContext transactionContext, long invoiceId,
        long returnId, int cashierId, long refund, CancellationToken ct)
    {
        var db = transactionContext;
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Thu hồi điểm phải cùng giao dịch đổi trả.");

        var invoice = await db.DocMotDongAsync("""
            SELECT khach_hang_id, diem_cong, so_tien_mot_diem, tong_thanh_toan, tien_tru_hang_doi
            FROM hoa_don WHERE id=@id FOR UPDATE
            """,
            r => new
            {
                Customer = r.IsDBNull(0) ? (uint?)null : Convert.ToUInt32(r[0]),
                Earned = Convert.ToInt32(r[1]),
                Rate = r.IsDBNull(2) ? 0u : Convert.ToUInt32(r[2]),
                Paid = Convert.ToInt64(r[3]),
                ExchangeCredit = Convert.ToInt64(r[4])
            }, ct, ("@id", invoiceId));
        if (invoice is null)
            throw LoiNghiepVu.KhongTimThay("Không tìm thấy hóa đơn.");
        if (invoice.Customer is not uint customer || invoice.Earned <= 0 || refund <= 0 || invoice.Paid <= 0)
            return 0;

        var existing = await db.DocMotGiaTriAsync("""
            SELECT so_diem FROM lich_su_diem
            WHERE phieu_doi_tra_id=@id AND loai='THU_HOI_TRA_HANG'
            """, ct, ("@id", returnId));
        if (existing is not null)
            return -Convert.ToInt32(existing);

        var previous = await db.DocMotDongAsync("""
            SELECT COALESCE(SUM(p.gia_tri_hang_tra),0), COALESCE(SUM(p.diem_thu_hoi),0)
            FROM phieu_doi_tra p
            WHERE p.id<>@return AND p.trang_thai='HOAN_TAT'
                AND p.hoa_don_goc_id=@invoice
            """, r => new { Refunded = Convert.ToInt64(r[0]), Recovered = Convert.ToInt32(r[1]) },
            ct, ("@return", returnId), ("@invoice", invoiceId));

        var refunded = Math.Min(invoice.Paid, (decimal)(previous?.Refunded ?? 0) + refund);
        // Reverse the points the customer would no longer earn on the retained sale.
        var retained = Math.Max(0m, invoice.Paid - refunded - invoice.ExchangeCredit);
        var remainingPoints = invoice.Rate > 0
            ? decimal.Floor(retained / invoice.Rate)
            : decimal.Ceiling(retained * invoice.Earned / invoice.Paid);
        var due = (int)Math.Max(0, invoice.Earned - remainingPoints - (previous?.Recovered ?? 0));
        if (due == 0)
            return 0;

        var balance = await db.DocMotGiaTriAsync("""
            SELECT diem_hien_co FROM khach_hang
            WHERE id=@id AND trang_thai='HOAT_DONG' FOR UPDATE
            """, ct, ("@id", customer));
        var recovered = (int)Math.Min(due, Convert.ToInt64(balance));
        if (recovered > 0)
        {
            await CustomerPoints.ChangeAsync(db, customer, -recovered, "THU_HOI_TRA_HANG",
                cashierId, checked((uint)invoiceId), checked((uint)returnId), "Thu hồi điểm khi trả hàng.", ct);
        }

        return recovered;
    }
}
