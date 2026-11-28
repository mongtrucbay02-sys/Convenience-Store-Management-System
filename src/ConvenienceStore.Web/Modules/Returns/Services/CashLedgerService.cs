
using System;
using System.Threading;
using System.Threading.Tasks;

using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Shared.Data;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Modules.Returns.Services;

/// <summary>
/// Sổ quỹ tiền mặt dùng chung cho POS và DT-04.
///
/// Quy tắc:
/// - Mọi thao tác ghi phải nằm trong transaction của nghiệp vụ gọi.
/// - Khóa dòng quỹ trước khi đọc và thay đổi số dư.
/// - Không cho phép số dư quỹ âm.
/// - Không tự commit hoặc rollback transaction của bên gọi.
/// </summary>
public sealed class CashLedgerService
{
    public sealed record Balance(long Amount, bool OpeningConfirmed);
    public sealed record Entry(string Kind, long Amount, long Before, long After, DateTime At, string Actor, string? Note);

    public async Task<Balance> GetBalanceAsync(CuaHangDbContext db, CancellationToken ct) =>
        await db.DocMotDongAsync("SELECT so_du,da_chot_so_du_dau FROM quy_tien_mat WHERE id=1",
            r => new Balance(Convert.ToInt64(r[0]), Convert.ToBoolean(r[1])), ct)
        ?? throw new LoiNghiepVu("CHUA_KHOI_TAO_QUY", 409, "Chưa khởi tạo quỹ tiền mặt.");

    public async Task<List<Entry>> GetEntriesAsync(CuaHangDbContext db, CancellationToken ct) =>
        await db.DocDanhSachAsync("""
            SELECT g.loai,g.so_tien,g.so_du_truoc,g.so_du_sau,g.thoi_diem,t.ho_ten,g.ghi_chu
            FROM giao_dich_tien_mat g JOIN tai_khoan t ON t.id=g.nguoi_thuc_hien_id
            ORDER BY g.id DESC LIMIT 100
            """, r => new Entry(r.GetString(0), Convert.ToInt64(r[1]), Convert.ToInt64(r[2]),
                Convert.ToInt64(r[3]), r.VietnamDateTime(4), r.GetString(5), r.IsDBNull(6) ? null : r.GetString(6)), ct);

    /// <summary>One-time physical count at upgrade. Never sum old invoices or reset the ledger.</summary>
    public async Task ConfirmOpeningAsync(CuaHangDbContext db, long actor, long counted, long expectedBalance, string? reason, CancellationToken ct)
    {
        reason = reason?.Trim();
        if (counted < 0 || expectedBalance < 0 || string.IsNullOrWhiteSpace(reason) || reason.Length > 500)
            throw LoiNghiepVu.DuLieuKhongHopLe("reason", "Số dư phải không âm; lý do bắt buộc, tối đa 500 ký tự.");
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, ct);
        var role = await db.DocMotGiaTriAsync("""
            SELECT v.ma FROM tai_khoan t JOIN vai_tro v ON v.id=t.vai_tro_id
            WHERE t.id=@actor AND t.trang_thai='HOAT_DONG' AND t.bat_buoc_doi_mk=FALSE FOR UPDATE
            """, ct, ("@actor", actor));
        if (!string.Equals(Convert.ToString(role), "AD", StringComparison.Ordinal))
            throw new LoiNghiepVu("KHONG_DU_QUYEN", 403, "Chỉ Admin được chốt số dư mở quỹ.");
        var before = await LockBalanceAsync(db, ct);
        var balance = await GetBalanceAsync(db, ct);
        if (balance.OpeningConfirmed)
            throw new LoiNghiepVu("DA_CHOT_SO_DU_DAU", 409, "Số dư mở quỹ đã được chốt; không được chốt lại.");
        if (before != expectedBalance)
            throw new LoiNghiepVu("QUY_DA_THAY_DOI", 409, "Quỹ đã thay đổi. Hãy tải lại và kiểm đếm trước khi chốt.");
        await db.ChayAsync("UPDATE quy_tien_mat SET so_du=@amount,da_chot_so_du_dau=TRUE WHERE id=1", ct, ("@amount", counted));
        await db.ChayAsync("""
            INSERT INTO giao_dich_tien_mat(loai,so_tien,so_du_truoc,so_du_sau,nguoi_thuc_hien_id,ghi_chu)
            VALUES ('CHOT_SO_DU_DAU',@difference,@before,@after,@actor,@reason)
            """, ct, ("@difference", Math.Abs(counted - before)), ("@before", before), ("@after", counted), ("@actor", actor), ("@reason", reason));
        await tx.CommitAsync(ct);
    }

    public async Task RecordCancellationRefundAsync(CuaHangDbContext db, long invoiceId, long actor, long amount, CancellationToken ct)
    {
        RequireTransaction(db);
        if (invoiceId <= 0 || actor <= 0 || amount <= 0)
            throw LoiNghiepVu.DuLieuKhongHopLe("amount", "Thông tin hoàn tiền hủy hóa đơn không hợp lệ.");
        var before = await LockBalanceAsync(db, ct);
        var existing = await db.DocMotGiaTriAsync("SELECT id FROM giao_dich_tien_mat WHERE loai='CHI_HUY_HOA_DON' AND hoa_don_id=@id", ct, ("@id", invoiceId));
        if (existing is not null)
            throw new LoiNghiepVu("DA_HOAN_TIEN_HUY", 409, "Hóa đơn đã được ghi chi hoàn tiền.");
        if (before < amount)
            throw new LoiNghiepVu("QUY_KHONG_DU", 409, "Quỹ tiền mặt không đủ để hoàn. Admin cần kiểm đếm và chốt số dư mở quỹ nếu chưa chốt.");
        await db.ChayAsync("UPDATE quy_tien_mat SET so_du=@after WHERE id=1", ct, ("@after", before - amount));
        await db.ChayAsync("""
            INSERT INTO giao_dich_tien_mat(loai,hoa_don_id,so_tien,so_du_truoc,so_du_sau,nguoi_thuc_hien_id)
            VALUES ('CHI_HUY_HOA_DON',@invoice,@amount,@before,@after,@actor)
            """, ct, ("@invoice", invoiceId), ("@amount", amount), ("@before", before), ("@after", before - amount), ("@actor", actor));
    }

    /// <summary>
    /// Ghi nhận doanh thu tiền mặt khi POS hoàn tất hóa đơn.
    /// Số tiền ghi thu là tổng thanh toán thực tế, không phải tiền khách đưa.
    /// </summary>
    public async Task RecordSaleAsync(
        CuaHangDbContext db,
        long invoiceId,
        long cashierId,
        long amount,
        CancellationToken ct)
    {
        RequireTransaction(db);

        if (invoiceId <= 0 || cashierId <= 0 || amount <= 0)
        {
            throw LoiNghiepVu.DuLieuKhongHopLe(
                "amount",
                "Thông tin giao dịch bán hàng không hợp lệ.");
        }

        // Khóa quỹ. Các giao dịch ghi quỹ khác phải chờ giao dịch hiện tại.
        long balance = await LockBalanceAsync(db, ct);

        // Chống ghi nhận doanh thu trùng cho cùng một hóa đơn.
        var existing = await db.DocMotGiaTriAsync(
            """
            SELECT id
            FROM giao_dich_tien_mat
            WHERE hoa_don_id = @invoice
              AND loai = 'THU_BAN_HANG'
            LIMIT 1
            """,
            ct,
            ("@invoice", invoiceId));

        if (existing is not null)
        {
            throw new LoiNghiepVu(
                "DA_GHI_NHAN_THU_BAN_HANG",
                409,
                "Hóa đơn này đã được ghi nhận doanh thu vào quỹ.");
        }

        long after;

        try
        {
            after = checked(balance + amount);
        }
        catch (OverflowException)
        {
            throw new LoiNghiepVu(
                "SO_DU_QUY_VUOT_GIOI_HAN",
                409,
                "Số dư quỹ vượt giới hạn lưu trữ.");
        }

        int updated = await db.ChayAsync(
            """
            UPDATE quy_tien_mat
            SET so_du = @after
            WHERE id = 1
            """,
            ct,
            ("@after", after));

        if (updated != 1)
        {
            throw new LoiNghiepVu(
                "KHONG_CAP_NHAT_DUOC_QUY",
                409,
                "Không thể cập nhật số dư quỹ tiền mặt.");
        }

        await db.ChayAsync(
            """
            INSERT INTO giao_dich_tien_mat
            (
                loai,
                hoa_don_id,
                phieu_doi_tra_id,
                so_tien,
                so_du_truoc,
                so_du_sau,
                nguoi_thuc_hien_id
            )
            VALUES
            (
                'THU_BAN_HANG',
                @invoice,
                NULL,
                @amount,
                @before,
                @after,
                @cashier
            )
            """,
            ct,
            ("@invoice", invoiceId),
            ("@amount", amount),
            ("@before", balance),
            ("@after", after),
            ("@cashier", cashierId));
    }

    /// <summary>
    /// Đọc và khóa dòng quỹ id=1.
    /// Chỉ được gọi trong transaction.
    /// </summary>
    internal async Task<long> LockBalanceAsync(
        CuaHangDbContext db,
        CancellationToken ct)
    {
        RequireTransaction(db);

        var value = await db.DocMotGiaTriAsync(
            """
            SELECT so_du
            FROM quy_tien_mat
            WHERE id = 1
            FOR UPDATE
            """,
            ct);

        if (value is null || value is DBNull)
        {
            throw new LoiNghiepVu(
                "CHUA_KHOI_TAO_QUY",
                409,
                "Chưa khởi tạo quỹ tiền mặt. Vui lòng liên hệ Admin.");
        }

        long balance;

        try
        {
            balance = Convert.ToInt64(value);
        }
        catch (Exception ex) when (
            ex is FormatException ||
            ex is InvalidCastException ||
            ex is OverflowException)
        {
            throw new LoiNghiepVu(
                "SO_DU_QUY_KHONG_HOP_LE",
                409,
                "Số dư quỹ trong database không hợp lệ.");
        }

        if (balance < 0)
        {
            throw new LoiNghiepVu(
                "SO_DU_QUY_KHONG_HOP_LE",
                409,
                "Số dư quỹ không được âm.");
        }

        return balance;
    }

    /// <summary>
    /// Bắt buộc nghiệp vụ ghi quỹ sử dụng transaction hiện tại.
    /// </summary>
    internal static void RequireTransaction(CuaHangDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);

        if (db.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "Thao tác quỹ tiền mặt phải nằm trong transaction hiện tại.");
        }
    }
}

/// <summary>
/// DT-04: hoàn tiền mặt cho khách sau khi phiếu trả hàng đủ điều kiện.
/// Dùng chung transaction với ReturnOrderService.
/// </summary>
public sealed class ReturnRefundService(CashLedgerService ledger)
{
    /// <summary>
    /// Ghi khoản chi hoàn tiền mặt.
    ///
    /// Điều kiện:
    /// - Có transaction.
    /// - Luồng thường dùng thu ngân lập phiếu; luồng duyệt dùng Admin đã duyệt và trực tiếp xác nhận chi.
    /// - Phiếu trả đã hoàn tất.
    /// - Người thực hiện trùng thu ngân lập phiếu.
    /// - Chưa có giao dịch hoàn tiền cho phiếu.
    /// - Quỹ đủ tiền hoàn.
    /// </summary>
    public Task<long> RefundAsync(CuaHangDbContext db, long returnId, long cashierId, CancellationToken ct = default) =>
        RefundCoreAsync(db, returnId, cashierId, false, ct);

    internal Task<long> RefundApprovedAsync(CuaHangDbContext db, long returnId, long adminId, CancellationToken ct) =>
        RefundCoreAsync(db, returnId, adminId, true, ct);

    private async Task<long> RefundCoreAsync(
        CuaHangDbContext db,
        long returnId,
        long cashierId,
        bool confirmedByApprover,
        CancellationToken ct)
    {
        CashLedgerService.RequireTransaction(db);

        if (returnId <= 0 || cashierId <= 0)
        {
            throw LoiNghiepVu.DuLieuKhongHopLe(
                "returnId",
                "Phiếu trả hoặc tài khoản thu ngân không hợp lệ.");
        }

        // 1. Kiểm tra tài khoản thực hiện.
        // Không chỉ dựa vào thông tin gửi từ trình duyệt.
        var validCashier = await db.DocMotGiaTriAsync(
            """
            SELECT t.id
            FROM tai_khoan t
            JOIN vai_tro v ON v.id = t.vai_tro_id
            WHERE t.id = @cashier
              AND v.ma = @role
              AND t.trang_thai = 'HOAT_DONG'
              AND t.bat_buoc_doi_mk = FALSE
            """,
            ct,
            ("@cashier", cashierId), ("@role", confirmedByApprover ? "AD" : "TN"));

        if (validCashier is null)
        {
            throw new LoiNghiepVu(
                "KHONG_DU_QUYEN",
                403,
                "Tài khoản xác nhận hoàn tiền không hợp lệ.");
        }

        // 2. Khóa phiếu trả để tránh hai yêu cầu xử lý cùng phiếu đồng thời.
        var slip = await db.DocMotDongAsync(
            """
            SELECT
                loai,
                trang_thai,
                tien_hoan,
                thu_ngan_id,
                nguoi_duyet_id
            FROM phieu_doi_tra
            WHERE id = @id
            FOR UPDATE
            """,
            r => new Slip(
                r.Chuoi(0),
                r.Chuoi(1),
                r.SoNguyenDai(2),
                r.SoNguyenDai(3), r.SoNguyenDaiHoacNull(4)),
            ct,
            ("@id", returnId));

        if (slip is null)
        {
            throw LoiNghiepVu.KhongTimThay(
                "Không tìm thấy phiếu trả hàng.");
        }

        // 3. Chỉ hoàn tiền cho phiếu trả hàng đã hoàn tất.
        if (slip.Loai != "TRA_HANG" ||
            slip.Status != "HOAN_TAT")
        {
            throw new LoiNghiepVu(
                "PHIEU_CHUA_DUOC_CHAP_NHAN",
                409,
                "Chỉ phiếu trả hàng đã được chấp nhận mới được hoàn tiền.");
        }

        // 4. Kiểm tra thu ngân lập phiếu và thu ngân hoàn tiền.
        if (confirmedByApprover ? slip.ApproverId != cashierId : slip.CashierId != cashierId)
        {
            throw new LoiNghiepVu(
                "KHONG_DU_QUYEN",
                403,
                "Thu ngân thực hiện không khớp với người lập phiếu trả.");
        }

        // 5. Kiểm tra số tiền.
        if (slip.Amount <= 0)
        {
            throw LoiNghiepVu.DuLieuKhongHopLe(
                "refund",
                "Số tiền hoàn phải lớn hơn 0.");
        }

        // 6. Kiểm tra phiếu đã được hoàn tiền chưa.
        var existing = await db.DocMotGiaTriAsync(
            """
            SELECT id
            FROM giao_dich_tien_mat
            WHERE phieu_doi_tra_id = @id
              AND loai = 'CHI_HOAN_TRA'
            LIMIT 1
            """,
            ct,
            ("@id", returnId));

        if (existing is not null)
        {
            throw new LoiNghiepVu(
                "DA_HOAN_TIEN",
                409,
                "Phiếu trả này đã được ghi nhận hoàn tiền.");
        }

        // 7. Khóa quỹ và đọc số dư mới nhất.
        // FOR UPDATE giúp các giao dịch thu/chi khác không sửa dòng quỹ
        // trong lúc nghiệp vụ hiện tại đang xử lý.
        long balance = await ledger.LockBalanceAsync(db, ct);

        // 8. Kiểm tra ngoại lệ E1.
        // Nếu thiếu tiền, ném lỗi để transaction bên gọi rollback.
        if (balance < slip.Amount)
        {
            throw new LoiNghiepVu(
                "QUY_TIEN_MAT_KHONG_DU",
                409,
                $"Quầy không đủ tiền mặt để hoàn {slip.Amount:N0} đ. " +
                $"Số dư hiện tại: {balance:N0} đ. " +
                "Vui lòng báo Admin/Quản lý xử lý trước khi xác nhận.");
        }

        long after = checked(balance - slip.Amount);

        if (after < 0)
        {
            throw new InvalidOperationException(
                "Số dư quỹ không được phép âm.");
        }

        // 9. Cập nhật số dư quỹ.
        int updated = await db.ChayAsync(
            """
            UPDATE quy_tien_mat
            SET so_du = @after
            WHERE id = 1
              AND so_du = @before
            """,
            ct,
            ("@after", after),
            ("@before", balance));

        if (updated != 1)
        {
            throw new LoiNghiepVu(
                "QUY_TIEN_MAT_THAY_DOI",
                409,
                "Số dư quỹ đã thay đổi. Vui lòng kiểm tra và thực hiện lại.");
        }

        // 10. Ghi khoản chi và liên kết với phiếu trả.
        await db.ChayAsync(
            """
            INSERT INTO giao_dich_tien_mat
            (
                loai,
                hoa_don_id,
                phieu_doi_tra_id,
                so_tien,
                so_du_truoc,
                so_du_sau,
                nguoi_thuc_hien_id
            )
            VALUES
            (
                'CHI_HOAN_TRA',
                NULL,
                @return,
                @amount,
                @before,
                @after,
                @cashier
            )
            """,
            ct,
            ("@return", returnId),
            ("@amount", slip.Amount),
            ("@before", balance),
            ("@after", after),
            ("@cashier", cashierId));

        // Không commit tại đây.
        // ReturnOrderService chịu trách nhiệm commit toàn bộ nghiệp vụ.
        return slip.Amount;
    }

    /// <summary>
    /// Đọc biên nhận hoàn tiền của thu ngân đang đăng nhập.
    /// </summary>
    public async Task<ReturnRefundReceipt> ReceiptAsync(
        CuaHangDbContext db,
        long returnId,
        long cashierId,
        CancellationToken ct = default)
    {
        if (returnId <= 0 || cashierId <= 0)
        {
            throw LoiNghiepVu.DuLieuKhongHopLe(
                "returnId",
                "Thông tin biên nhận không hợp lệ.");
        }

        var result = await db.DocMotDongAsync(
            """
            SELECT
                p.id,
                p.ma_phieu,
                p.hoa_don_goc_id,
                h.ma_hoa_don,
                g.so_tien,
                g.thoi_diem,
                g.so_du_sau,
                t.ho_ten
            FROM phieu_doi_tra p
            JOIN hoa_don h
                ON h.id = p.hoa_don_goc_id
            JOIN giao_dich_tien_mat g
                ON g.phieu_doi_tra_id = p.id
               AND g.loai = 'CHI_HOAN_TRA'
            JOIN tai_khoan t
                ON t.id = g.nguoi_thuc_hien_id
            WHERE p.id = @id
              AND p.loai = 'TRA_HANG'
              AND p.trang_thai = 'HOAN_TAT'
              AND p.thu_ngan_id = @cashier
              AND (g.nguoi_thuc_hien_id = @cashier OR g.nguoi_thuc_hien_id = p.nguoi_duyet_id)
            """,
            r => new ReturnRefundReceipt(
                r.SoNguyenDai(0),
                r.Chuoi(1),
                r.SoNguyenDai(2),
                r.Chuoi(3),
                r.SoNguyenDai(4),
                r.NgayGio(5),
                r.SoNguyenDai(6),
                r.Chuoi(7)),
            ct,
            ("@id", returnId),
            ("@cashier", cashierId));

        return result ?? throw LoiNghiepVu.KhongTimThay(
            "Không tìm thấy giao dịch hoàn tiền của bạn.");
    }

    private sealed record Slip(
        string Loai,
        string Status,
        long Amount,
        long CashierId,
        long? ApproverId);
}

/// <summary>
/// Dữ liệu biên nhận hoàn tiền mặt.
/// </summary>
public sealed record ReturnRefundReceipt(
    long ReturnId,
    string ReturnCode,
    long InvoiceId,
    string InvoiceCode,
    long Amount,
    DateTime CreatedAt,
    long BalanceAfter,
    string Cashier);
