
using System;
using System.Threading;
using System.Threading.Tasks;

using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Shared.Data;
using ConvenienceStore.Web.Shared.Errors;

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
    /// <summary>
    /// Ghi nhận doanh thu tiền mặt khi POS hoàn tất hóa đơn.
    /// Số tiền ghi thu là tổng thanh toán thực tế, không phải tiền khách đưa.
    /// </summary>
    public async Task RecordSaleAsync(
        CuaHangDbContext db,
        long invoiceId,
        int cashierId,
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
    /// - Tài khoản thực hiện là thu ngân hợp lệ.
    /// - Phiếu trả đã hoàn tất.
    /// - Người thực hiện trùng thu ngân lập phiếu.
    /// - Chưa có giao dịch hoàn tiền cho phiếu.
    /// - Quỹ đủ tiền hoàn.
    /// </summary>
    public async Task<long> RefundAsync(
        CuaHangDbContext db,
        long returnId,
        int cashierId,
        CancellationToken ct = default)
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
              AND v.ma = 'TN'
              AND t.trang_thai = 'HOAT_DONG'
              AND t.bat_buoc_doi_mk = FALSE
            """,
            ct,
            ("@cashier", cashierId));

        if (validCashier is null)
        {
            throw new LoiNghiepVu(
                "KHONG_DU_QUYEN",
                403,
                "Chỉ thu ngân hợp lệ mới được xác nhận hoàn tiền.");
        }

        // 2. Khóa phiếu trả để tránh hai yêu cầu xử lý cùng phiếu đồng thời.
        var slip = await db.DocMotDongAsync(
            """
            SELECT
                loai,
                trang_thai,
                tien_hoan,
                thu_ngan_id
            FROM phieu_doi_tra
            WHERE id = @id
            FOR UPDATE
            """,
            r => new Slip(
                r.Chuoi(0),
                r.Chuoi(1),
                r.SoNguyenDai(2),
                r.SoNguyen(3)),
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
        if (slip.CashierId != cashierId)
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
        int cashierId,
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
              AND g.nguoi_thuc_hien_id = @cashier
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
        int CashierId);
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
