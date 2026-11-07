using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Shared.Data;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Modules.Customers;

/// <summary>Shared append-only ledger for checkout, returns and invoice cancellation. Caller owns the transaction.</summary>
public static class CustomerPoints
{
    public static async Task<uint> ChangeAsync(CuaHangDbContext db, uint customerId, int delta,
        string type, int actor, uint? invoiceId, uint? returnId, string? reason, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction == null)
            throw new InvalidOperationException("Thay đổi điểm phải cùng giao dịch nghiệp vụ.");
        bool valid = type switch {
            "TICH_DIEM" => delta > 0 && invoiceId.HasValue && !returnId.HasValue,
            "THU_HOI_HUY_HOA_DON" => delta < 0 && invoiceId.HasValue && !returnId.HasValue,
            "THU_HOI_TRA_HANG" => delta < 0 && invoiceId.HasValue && returnId.HasValue,
            "DIEU_CHINH" => delta != 0 && !string.IsNullOrWhiteSpace(reason) && reason.Length <= 500,
            _ => false
        };
        if (!valid) throw LoiNghiepVu.DuLieuKhongHopLe("points", "Biến động điểm không hợp lệ.");
        var current = await db.DocMotGiaTriAsync("SELECT diem_hien_co FROM khach_hang WHERE id=@id AND trang_thai='HOAT_DONG' FOR UPDATE", ct, ("@id", customerId));
        if (current == null) throw LoiNghiepVu.KhongTimThay("Khách hàng không còn hoạt động.");
        // Customer row lock serializes retries. One business event must not award/revoke twice.
        if (type != "DIEU_CHINH")
        {
            var existing = await db.DocMotGiaTriAsync("""
                SELECT so_diem FROM lich_su_diem WHERE khach_hang_id=@id AND loai=@type
                AND hoa_don_id=@invoice AND phieu_doi_tra_id <=> @return LIMIT 1
                """, ct, ("@id", customerId), ("@type", type), ("@invoice", invoiceId), ("@return", returnId));
            if (existing != null)
            {
                if (Convert.ToInt32(existing) != delta)
                    throw LoiNghiepVu.XungDot(MaLoiChung.XungDotDuLieu, "Sự kiện đã ghi với số điểm khác.");
                return Convert.ToUInt32(current);
            }
        }
        uint next;
        try { next = CustomerRules.AdjustedBalance(Convert.ToUInt32(current), delta); }
        catch (ArgumentOutOfRangeException ex) { throw LoiNghiepVu.DuLieuKhongHopLe("points", ex.Message); }
        await db.ChayAsync("UPDATE khach_hang SET diem_hien_co=@p,phien_ban=phien_ban+1 WHERE id=@id", ct, ("@p", next), ("@id", customerId));
        await db.ChayAsync("""
            INSERT INTO lich_su_diem (khach_hang_id,loai,so_diem,diem_sau,hoa_don_id,phieu_doi_tra_id,ly_do,nguoi_thuc_hien_id)
            VALUES (@id,@type,@delta,@after,@invoice,@return,@reason,@actor)
            """, ct, ("@id", customerId), ("@type", type), ("@delta", delta), ("@after", next),
            ("@invoice", invoiceId), ("@return", returnId), ("@reason", reason), ("@actor", actor));
        return next;
    }
}
