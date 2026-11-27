using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Modules.Inventory.Models;
using ConvenienceStore.Web.Modules.Inventory.Services;
using ConvenienceStore.Web.Services.Pos;
using ConvenienceStore.Web.Shared.Api;
using ConvenienceStore.Web.Shared.Data;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace ConvenienceStore.Web.Modules.Returns.Services;

public sealed record ApprovalSlip(long Id, string Code, string Kind, string Status, long InvoiceId,
    string InvoiceCode, long CashierId, string Cashier, long? CustomerId, long ReturnValue,
    long CashDue, bool Expired, bool Forbidden, bool OverThreshold, string? ApprovalNote, bool InvoicePaid,
    string? ExchangePlan = null);

public sealed record ApprovalLine(long ProductId, string Name, int Quantity, long UnitPaid,
    string Reason, string? Note, string? Condition, string Handling, decimal Cost);

public sealed record ApprovalDetails(ApprovalSlip Slip, IReadOnlyList<ApprovalLine> Lines);

public sealed class ReturnApprovalService(CuaHangDbContext db, IDichVuKho stock, IReturnPointsGateway points,
    ReturnRefundService refunds, ExchangeService exchange, TimeProvider time)
{
    private const string SlipQuery = """
        SELECT p.id, p.ma_phieu, p.loai, p.trang_thai, p.hoa_don_goc_id, h.ma_hoa_don,
               p.thu_ngan_id, t.ho_ten, p.khach_hang_id, p.gia_tri_hang_tra, p.tien_thu_them,
               p.can_duyet_qua_han, p.can_duyet_danh_muc, p.can_duyet_vuot_nguong, p.ghi_chu_duyet, h.trang_thai,
               p.phuong_an_doi::text
        FROM phieu_doi_tra p
        JOIN hoa_don h ON h.id = p.hoa_don_goc_id
        JOIN tai_khoan t ON t.id = p.thu_ngan_id
        """;

    private DateTime Now => time.GetUtcNow().ToOffset(GioVietNam.DoLech).DateTime;

    public Task<List<ApprovalSlip>> PendingAsync(CancellationToken ct) =>
        db.DocDanhSachAsync(SlipQuery + " WHERE p.trang_thai = 'CHO_DUYET' ORDER BY p.ngay_lap, p.id", ReadSlip, ct);

    public Task<List<ApprovalSlip>> ApprovedExchangesAsync(long cashier, CancellationToken ct) =>
        db.DocDanhSachAsync(SlipQuery + " WHERE p.trang_thai = 'DA_DUYET' AND p.loai = 'DOI_HANG' AND p.thu_ngan_id = @actor ORDER BY p.duyet_luc, p.id",
            ReadSlip, ct, ("@actor", cashier));

    public async Task<ApprovalDetails> DetailsAsync(long id, long? cashier, CancellationToken ct)
    {
        var slip = await LoadAsync(id, ct);
        if (cashier.HasValue && (slip.CashierId != cashier || slip.Kind != "DOI_HANG"))
            throw Missing();
        return new(slip, await LinesAsync(id, ct));
    }

    public async Task DecideAsync(long id, long admin, bool approve, string? note, bool refundConfirmed, CancellationToken ct)
    {
        note = note?.Trim();
        if (string.IsNullOrWhiteSpace(note) || note.Length > 255)
            throw new LoiNghiepVu("GHI_CHU_DUYET_KHONG_HOP_LE", 422, "Nhập ghi chú duyệt hoặc từ chối, tối đa 255 ký tự.");

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var slip = await LockAsync(id, ct);
        await ExchangePlanHistory.RequireActorAsync(db, admin, "AD", ct);
        if (slip.Status != "CHO_DUYET")
            throw Conflict("Phiếu đã được xử lý. Thông tin đã được tải lại.");
        if (approve && !slip.InvoicePaid)
            throw Conflict("Hóa đơn gốc không còn hợp lệ để duyệt.");

        if (slip.Kind == "DOI_HANG")
        {
            var history = ExchangePlanHistory.Read(slip.ExchangePlan);
            if (approve && history is null)
                throw ExchangePlanHistory.Changed("Phiếu cũ chưa có snapshot. Thu ngân phải gửi phương án trước khi duyệt.");
            if (history is not null)
                await SavePlanAsync(id, history.Decide(admin, time.GetUtcNow(), approve, note), ct);
        }

        bool completeReturn = approve && slip.Kind == "TRA_HANG";
        if (completeReturn && slip.ReturnValue > 0 && !refundConfirmed)
            throw new LoiNghiepVu("CHUA_XAC_NHAN_HOAN_TIEN", 422, "Cần xác nhận hoàn tiền tại quầy trước khi duyệt phiếu trả.");

        var now = Now;
        string status = !approve ? "BI_TU_CHOI" : completeReturn ? "HOAN_TAT" : "DA_DUYET";
        await db.ChayAsync("""
            UPDATE phieu_doi_tra
            SET trang_thai = @status, nguoi_duyet_id = @admin, duyet_luc = @now,
                ghi_chu_duyet = @note, hoan_tat_luc = @completed
            WHERE id = @id
            """, ct, ("@status", status), ("@admin", admin), ("@now", now), ("@note", note),
            ("@completed", completeReturn ? now : (DateTime?)null), ("@id", id));

        if (completeReturn)
        {
            await RecordReturnEffectsAsync(slip, admin, ct);
            if (slip.ReturnValue > 0)
                await refunds.RefundApprovedAsync(db, id, admin, ct);
        }
        await tx.CommitAsync(ct);
    }

    public async Task CompleteExchangeAsync(long id, long cashier, long cashReceived, bool cashConfirmed, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var slip = await LockAsync(id, ct);
        await ExchangePlanHistory.RequireActorAsync(db, cashier, "TN", ct);
        if (slip.Kind != "DOI_HANG" || slip.CashierId != cashier)
            throw Missing();
        if (slip.Status != "DA_DUYET" || !slip.InvoicePaid)
            throw Conflict("Phiếu không còn ở trạng thái có thể hoàn tất. Thông tin đã được tải lại.");

        var history = ExchangePlanHistory.Read(slip.ExchangePlan)
            ?? throw ExchangePlanHistory.Changed("Chưa có snapshot phương án. Cần gửi lại và duyệt lại.");
        var sale = await exchange.CheckoutApprovedCartAsync(cashier, slip.CustomerId, slip.ReturnValue,
            slip.CashDue, cashReceived, cashConfirmed, history.Current, ct);
        await db.ChayAsync("""
            UPDATE phieu_doi_tra
            SET trang_thai = 'HOAN_TAT', hoa_don_moi_id = @invoice, hoan_tat_luc = @now
            WHERE id = @id
            """, ct, ("@invoice", sale.InvoiceId), ("@now", Now), ("@id", id));
        await RecordReturnEffectsAsync(slip, cashier, ct);
        await tx.CommitAsync(ct);
    }

    private async Task RecordReturnEffectsAsync(ApprovalSlip slip, long performedBy, CancellationToken ct)
    {
        var restock = (await LinesAsync(slip.Id, ct)).Where(l => l.Handling == "NHAP_LAI")
            .Select(l => new DongBienDong(l.ProductId, SoLuong: l.Quantity, GiaVon: l.Cost)).ToList();
        if (restock.Count > 0)
            await stock.GhiNhanBienDongAsync(new(LoaiBienDong.TRA_HANG, slip.Id,
                slip.Code, performedBy, restock), ct);

        int recovered = await points.RecoverAsync(db, slip.InvoiceId, slip.Id, performedBy, slip.ReturnValue, ct);
        await db.ChayAsync("UPDATE phieu_doi_tra SET diem_thu_hoi = @points WHERE id = @id",
            ct, ("@points", recovered), ("@id", slip.Id));
    }

    private async Task<ApprovalSlip> LockAsync(long id, CancellationToken ct)
    {
        var slip = await LoadAsync(id, ct);
        // Luồng lập phiếu cũng khóa hóa đơn gốc trước khi cập nhật dữ liệu đổi trả.
        await db.DocMotGiaTriAsync("SELECT id FROM hoa_don WHERE id = @id FOR UPDATE", ct, ("@id", slip.InvoiceId));
        return await db.DocMotDongAsync(SlipQuery + " WHERE p.id = @id FOR UPDATE OF p", ReadSlip, ct, ("@id", id))
            ?? throw Missing();
    }

    private async Task<ApprovalSlip> LoadAsync(long id, CancellationToken ct) =>
        await db.DocMotDongAsync(SlipQuery + " WHERE p.id = @id", ReadSlip, ct, ("@id", id)) ?? throw Missing();

    private Task<List<ApprovalLine>> LinesAsync(long id, CancellationToken ct) => db.DocDanhSachAsync("""
        SELECT c.san_pham_id, d.ten_san_pham, c.so_luong, c.don_gia_thuc_tra,
               c.ly_do, c.ghi_chu_ly_do, c.tinh_trang, c.cach_xu_ly, c.gia_von
        FROM ct_phieu_doi_tra c
        JOIN ct_hoa_don d ON d.hoa_don_id = c.hoa_don_id AND d.san_pham_id = c.san_pham_id
        WHERE c.phieu_doi_tra_id = @id ORDER BY c.san_pham_id
        """, r => new ApprovalLine(r.SoNguyenDai(0), r.Chuoi(1), r.SoNguyen(2), r.SoNguyenDai(3),
        r.Chuoi(4), r.ChuoiHoacNull(5), r.ChuoiHoacNull(6), r.Chuoi(7), Convert.ToDecimal(r[8])), ct, ("@id", id));

    private static ApprovalSlip ReadSlip(System.Data.Common.DbDataReader r) =>
        new(r.SoNguyenDai(0), r.Chuoi(1), r.Chuoi(2), r.Chuoi(3), r.SoNguyenDai(4), r.Chuoi(5),
            r.SoNguyenDai(6), r.Chuoi(7), r.SoNguyenDaiHoacNull(8), r.SoNguyenDai(9),
            r.SoNguyenDai(10), r.DungSai(11), r.DungSai(12), r.DungSai(13), r.ChuoiHoacNull(14),
            r.Chuoi(15) == "DA_THANH_TOAN", r.ChuoiHoacNull(16));

    private Task<int> SavePlanAsync(long id, ExchangePlanHistory history, CancellationToken ct) =>
        db.ChayAsync("UPDATE phieu_doi_tra SET phuong_an_doi=CAST(@plan AS jsonb) WHERE id=@id",
            ct, ("@id", id), ("@plan", history.Json()));

    public async Task<ExchangePlanResubmission> PreviewResubmissionAsync(long id, long cashier, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var slip = await ResubmissionSlipAsync(id, cashier, ct);
        var revision = (ExchangePlanHistory.Read(slip.ExchangePlan)?.Current.Revision ?? 0) + 1;
        var proposal = await exchange.CapturePlanAsync(cashier, slip.CustomerId, slip.ReturnValue, revision, ct);
        var token = ResubmissionToken(slip, proposal);
        await tx.CommitAsync(ct);
        return new(new(slip, await LinesAsync(id, ct)), proposal, token);
    }

    public async Task ResubmitAsync(long id, long cashier, string? token, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var slip = await ResubmissionSlipAsync(id, cashier, ct);
        var history = ExchangePlanHistory.Read(slip.ExchangePlan);
        var revision = (history?.Current.Revision ?? 0) + 1;
        if (revision > 100) throw Conflict("Đã đạt giới hạn phiên bản. Vui lòng liên hệ Admin.");
        var proposal = await exchange.CapturePlanAsync(cashier, slip.CustomerId, slip.ReturnValue, revision, ct);
        if (string.IsNullOrEmpty(token) || token != ResubmissionToken(slip, proposal))
            throw ExchangePlanHistory.Changed("Phiếu hoặc giỏ đã thay đổi. Xem lại phương án trước khi gửi.");
        var legacy = history?.LegacyDecision;
        if (history is null)
            legacy = await db.DocMotDongAsync("""
                SELECT trang_thai, nguoi_duyet_id, duyet_luc, ghi_chu_duyet
                FROM phieu_doi_tra WHERE id=@id AND nguoi_duyet_id IS NOT NULL
                """, r => new LegacyExchangeDecision(r.Chuoi(0), r.SoNguyenDaiHoacNull(1),
                    r.NgayGioHoacNull(2), r.ChuoiHoacNull(3)), ct, ("@id", id));
        await SavePlanAsync(id, new(1, (history?.Versions ?? []).Append(proposal).ToArray(), legacy), ct);
        await db.ChayAsync("""
            UPDATE phieu_doi_tra SET trang_thai='CHO_DUYET', tien_thu_them=@cash,
              nguoi_duyet_id=NULL, duyet_luc=NULL, ghi_chu_duyet=NULL WHERE id=@id
            """, ct, ("@id", id), ("@cash", proposal.CashDue));
        await tx.CommitAsync(ct);
    }

    private async Task<ApprovalSlip> ResubmissionSlipAsync(long id, long cashier, CancellationToken ct)
    {
        var slip = await LockAsync(id, ct);
        await ExchangePlanHistory.RequireActorAsync(db, cashier, "TN", ct);
        if (slip.Kind != "DOI_HANG" || slip.CashierId != cashier) throw Missing();
        if (!slip.InvoicePaid || slip.Status is not ("CHO_DUYET" or "DA_DUYET"))
            throw Conflict("Phiếu không thể gửi lại phương án.");
        return slip;
    }

    private static string ResubmissionToken(ApprovalSlip slip, ExchangePlanVersion proposal) =>
        slip.Id + ":" + slip.Status + ":" + ExchangePlanHistory.Fingerprint(proposal);

    private static LoiNghiepVu Missing() => LoiNghiepVu.KhongTimThay("Không tìm thấy phiếu đổi trả.");
    private static LoiNghiepVu Conflict(string message) => LoiNghiepVu.XungDot("DT06_XUNG_DOT", message);
}
