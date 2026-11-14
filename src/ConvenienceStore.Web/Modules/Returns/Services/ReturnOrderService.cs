using System.Data;
using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Modules.Inventory.Models;
using ConvenienceStore.Web.Modules.Inventory.Services;
using ConvenienceStore.Web.Modules.Returns.Models;
using ConvenienceStore.Web.Shared.Api;
using ConvenienceStore.Web.Shared.Data;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Modules.Returns.Services;

public sealed class ReturnOrderService(CuaHangDbContext db, IDichVuKho stock,
    IReturnPointsGateway points, TimeProvider time)
{
    private sealed record Header(long Id, string Code, string Status, DateTime PurchasedAt,
        int? CustomerId, int PointsEarned);
    private sealed record Policy(int Id, int Days, long Threshold);
    private sealed record Line(int Id, string Name, string Unit, int Quantity, long Price, long Discount,
        decimal Cost, bool Forbidden, long Reserved);
    private sealed record Draft(Header Invoice, Policy Policy, List<Line> Original, ReturnPreview Preview);
    private static LoiNghiepVu Conflict(string message) => new("DOI_TRA_XUNG_DOT", 409, message);
    private static readonly HashSet<string> Reasons = ["LOI_SAN_PHAM", "HET_HAN", "KHONG_DUNG_NHU_CAU", "KHAC"];
    private static readonly HashSet<string> Handlings = ["NHAP_LAI", "HUY"];
    private DateTime Now => time.GetUtcNow().ToOffset(GioVietNam.DoLech).DateTime;

    private async Task<int> Actor(ClaimsPrincipal user, CancellationToken ct)
    {
        ReturnLookupAccess.Require(user);
        int id = user.LayTaiKhoanId();
        var found = await db.DocMotGiaTriAsync("""
            SELECT t.id FROM tai_khoan t JOIN vai_tro v ON v.id=t.vai_tro_id
            WHERE t.id=@id AND v.ma='TN' AND t.trang_thai='HOAT_DONG' AND t.bat_buoc_doi_mk=FALSE
            """, ct, ("@id", id));
        if (found is null) throw new LoiNghiepVu("KHONG_DU_QUYEN", 403, "Tài khoản thu ngân không hợp lệ.");
        return id;
    }
    private async Task<Header> Invoice(long id, CancellationToken ct)
    {
        if (id <= 0) throw LoiNghiepVu.DuLieuKhongHopLe("invoiceId", "Mã hóa đơn không hợp lệ.");
        var h = await db.DocMotDongAsync("""
            SELECT id,ma_hoa_don,trang_thai,thanh_toan_luc,khach_hang_id,diem_cong
            FROM hoa_don WHERE id=@id FOR UPDATE
            """, r => new Header(r.SoNguyenDai(0), r.Chuoi(1), r.Chuoi(2), r.NgayGio(3),
                r.IsDBNull(4) ? null : r.SoNguyen(4), r.SoNguyen(5)), ct, ("@id", id));
        if (h is null) throw LoiNghiepVu.KhongTimThay("Không tìm thấy hóa đơn gốc.");
        if (h.Status != "DA_THANH_TOAN" || h.PurchasedAt > Now) throw Conflict("Hóa đơn đã hủy hoặc không hợp lệ, không được trả hàng.");
        return h;
    }
    private Task<List<Line>> Lines(long id, CancellationToken ct) => db.DocDanhSachAsync("""
        SELECT d.san_pham_id,d.ten_san_pham,d.don_vi_tinh,d.so_luong,d.don_gia,d.giam_don_vi,d.gia_von,
            dm.khong_doi_tra,COALESCE((SELECT SUM(c.so_luong) FROM ct_phieu_doi_tra c
                JOIN phieu_doi_tra p ON p.id=c.phieu_doi_tra_id WHERE c.hoa_don_id=d.hoa_don_id
                AND c.san_pham_id=d.san_pham_id AND p.trang_thai IN ('CHO_DUYET','DA_DUYET','HOAN_TAT')),0)
        FROM ct_hoa_don d JOIN san_pham sp ON sp.id=d.san_pham_id
        JOIN danh_muc dm ON dm.id=sp.danh_muc_id WHERE d.hoa_don_id=@id ORDER BY d.san_pham_id
        """, r => new Line(r.SoNguyen(0), r.Chuoi(1), r.Chuoi(2), r.SoNguyen(3), r.SoNguyenDai(4),
            r.SoNguyenDai(5), Convert.ToDecimal(r[6]), r.DungSai(7), r.SoNguyenDai(8)), ct, ("@id", id));

    public async Task<ReturnForm> FormAsync(long invoiceId, ClaimsPrincipal user, CancellationToken ct = default)
    {
        await Actor(user, ct);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var h = await Invoice(invoiceId, ct); var lines = await Lines(invoiceId, ct);
        var form = new ReturnForm(h.Id, h.Code, GioVietNam.ThanhOffset(h.PurchasedAt), lines.Select(l => new ReturnFormLine(l.Id, l.Name, l.Unit,
            l.Price - l.Discount, l.Quantity, Math.Max(0, l.Quantity - l.Reserved), l.Forbidden)).ToList());
        await tx.CommitAsync(ct); return form;
    }

    private async Task<Draft> Calculate(ReturnOrderRequest request, int actor, CancellationToken ct)
    {
        if (request.Lines is null || request.Lines.Count is 0 or > 100 || request.Lines.Any(l => l is null))
            throw LoiNghiepVu.DuLieuKhongHopLe("lines", "Chọn từ 1 đến 100 dòng trả hàng.");
        var selected = request.Lines.Where(l => l.Quantity != 0).OrderBy(l => l.ProductId).ToList();
        if (selected.Count == 0 || selected.Any(l => l.Quantity < 0 || l.ProductId <= 0)
            || selected.Select(l => l.ProductId).Distinct().Count() != selected.Count)
            throw LoiNghiepVu.DuLieuKhongHopLe("lines", "Số lượng phải dương, không được lặp sản phẩm.");
        foreach (var l in selected)
        {
            if (!Reasons.Contains(l.Reason ?? "")) throw LoiNghiepVu.DuLieuKhongHopLe("reason", "Chọn lý do trả hàng hợp lệ.");
            if (!Handlings.Contains(l.Handling ?? "")) throw LoiNghiepVu.DuLieuKhongHopLe("handling", "Chọn nhập lại kho hoặc hủy.");
            if (string.IsNullOrWhiteSpace(l.Condition) || l.Condition.Length > 255)
                throw LoiNghiepVu.DuLieuKhongHopLe("condition", "Nhập tình trạng hàng, tối đa 255 ký tự.");
            if (l.Note?.Length > 255 || (l.Reason == "KHAC" && string.IsNullOrWhiteSpace(l.Note)))
                throw LoiNghiepVu.DuLieuKhongHopLe("note", "Lý do khác cần ghi chú, tối đa 255 ký tự.");
        }
        var h = await Invoice(request.InvoiceId, ct); var original = await Lines(h.Id, ct); var now = Now;
        var policy = await db.DocMotDongAsync("""
            SELECT id,so_ngay_doi_tra,nguong_tien_can_duyet FROM chinh_sach_doi_tra
            WHERE hieu_luc_tu<=@now ORDER BY hieu_luc_tu DESC,id DESC LIMIT 1 FOR SHARE
            """, r => new Policy(r.SoNguyen(0), r.SoNguyen(1), r.SoNguyenDai(2)), ct, ("@now", now))
            ?? throw Conflict("Chưa có chính sách đổi trả đang hiệu lực.");
        bool expired = (now.Date - h.PurchasedAt.Date).TotalDays > policy.Days;
        var result = new List<ReturnPreviewLine>(); long refund = 0;
        foreach (var l in selected)
        {
            var saved = original.SingleOrDefault(x => x.Id == l.ProductId);
            if (saved is null) throw LoiNghiepVu.DuLieuKhongHopLe("productId", "Sản phẩm không thuộc hóa đơn gốc.");
            long remaining = Math.Max(0, (long)saved.Quantity - saved.Reserved);
            if (l.Quantity > remaining) throw LoiNghiepVu.DuLieuKhongHopLe("quantity", $"{saved.Name}: chỉ còn {remaining} sản phẩm được trả.");
            long paid = saved.Price - saved.Discount;
            if (paid < 0) throw Conflict("Dòng hóa đơn có giá thực trả không hợp lệ.");
            refund = checked(refund + paid * l.Quantity);
            result.Add(new(saved.Id, saved.Name, l.Quantity, paid, l.Reason, l.Note?.Trim(), l.Condition.Trim(),
                l.Handling, expired || saved.Forbidden, saved.Cost));
        }
        bool forbidden = result.Any(l => original.Single(o => o.Id == l.ProductId).Forbidden);
        bool over = refund > policy.Threshold, needs = expired || forbidden || over;
        // Fingerprint binds confirmation to server prices, policy, remaining quantities, actor and selected data.
        string token = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(
            new { actor, h.Id, h.Status, h.CustomerId, h.PointsEarned, policy, expired, original, result, refund }))));
        return new(h, policy, original, new(h.Id, h.Code, refund, expired, forbidden, over, needs,
            h.PointsEarned > 0 && refund > 0, token, result));
    }
    public async Task<ReturnPreview> PreviewAsync(ReturnOrderRequest request, ClaimsPrincipal user, CancellationToken ct = default)
    {
        int actor = await Actor(user, ct);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var draft = await Calculate(request, actor, ct); await tx.CommitAsync(ct); return draft.Preview;
    }
    public async Task<long> CreateAsync(ReturnOrderRequest request, ClaimsPrincipal user, CancellationToken ct = default)
    {
        int actor = await Actor(user, ct);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var draft = await Calculate(request, actor, ct); var preview = draft.Preview;
        if (string.IsNullOrEmpty(request.ConfirmationToken) || request.ConfirmationToken != preview.ConfirmationToken)
            throw Conflict("Điều kiện hoặc số lượng còn trả đã thay đổi. Vui lòng tính lại và xác nhận.");
        if (!preview.NeedsApproval && !request.CashConfirmed)
            throw LoiNghiepVu.DuLieuKhongHopLe("cashConfirmed", "Cần xác nhận hoàn tiền mặt cho khách.");
        // Phiếu chờ duyệt chưa thực hiện hoàn tiền, cập nhật kho hoặc thu hồi điểm.
        string period = Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        await db.ChayAsync("INSERT INTO bo_dem_chung_tu (tien_to,ky,so_cuoi) VALUES ('TH',@ky,1) ON DUPLICATE KEY UPDATE so_cuoi=so_cuoi+1", ct, ("@ky", period));
        long serial = Convert.ToInt64(await db.DocMotGiaTriAsync("SELECT so_cuoi FROM bo_dem_chung_tu WHERE tien_to='TH' AND ky=@ky", ct, ("@ky", period)));
        string code = $"TH-{period}-{serial:D3}";
        if (code.Length > 20) throw Conflict("Đã vượt giới hạn mã phiếu trong ngày.");
        await db.ChayAsync("""
            INSERT INTO phieu_doi_tra (ma_phieu,loai,hoa_don_goc_id,khach_hang_id,chinh_sach_id,trang_thai,
                can_duyet_qua_han,can_duyet_danh_muc,can_duyet_vuot_nguong,gia_tri_hang_tra,tien_hoan,
                thu_ngan_id,hoan_tat_luc)
            VALUES (@ma,'TRA_HANG',@hd,@kh,@cs,@tt,@qh,@dm,@vn,@tien,@tien,@tn,@done)
            """, ct, ("@ma", code), ("@hd", draft.Invoice.Id), ("@kh", draft.Invoice.CustomerId), ("@cs", draft.Policy.Id),
            ("@tt", preview.NeedsApproval ? "CHO_DUYET" : "HOAN_TAT"), ("@qh", preview.Expired), ("@dm", preview.Forbidden),
            ("@vn", preview.OverThreshold), ("@tien", preview.Refund), ("@tn", actor), ("@done", preview.NeedsApproval ? null : Now));
        long id = Convert.ToInt64(await db.DocMotGiaTriAsync("SELECT LAST_INSERT_ID()", ct));
        foreach (var l in preview.Lines)
            await db.ChayAsync("""
                INSERT INTO ct_phieu_doi_tra (phieu_doi_tra_id,san_pham_id,hoa_don_id,so_luong,don_gia_thuc_tra,
                    ly_do,ghi_chu_ly_do,tinh_trang,cach_xu_ly,ngoai_le,gia_von)
                VALUES (@p,@sp,@hd,@sl,@gia,@ld,@gc,@tt,@xl,@nl,@gv)
                """, ct, ("@p", id), ("@sp", l.ProductId), ("@hd", draft.Invoice.Id), ("@sl", l.Quantity), ("@gia", l.UnitPaid),
                ("@ld", l.Reason), ("@gc", l.Note), ("@tt", l.Condition), ("@xl", l.Handling), ("@nl", l.Exception), ("@gv", l.Cost));
        if (!preview.NeedsApproval)
        {
            var restock = preview.Lines.Where(l => l.Handling == "NHAP_LAI")
                .Select(l => new DongBienDong(l.ProductId, SoLuong: l.Quantity, GiaVon: l.Cost)).ToList();
            if (restock.Count > 0) await stock.GhiNhanBienDongAsync(new(LoaiBienDong.TRA_HANG, checked((int)id), code, actor, restock), ct);
            if (preview.PointsBlocked)
            {
                int recovered = await points.RecoverAsync(db, draft.Invoice.Id, id, actor, preview.Refund, ct);
                if (recovered < 0) throw new InvalidOperationException("KH-06 trả số điểm không hợp lệ.");
                await db.ChayAsync("UPDATE phieu_doi_tra SET diem_thu_hoi=@d WHERE id=@id", ct, ("@d", recovered), ("@id", id));
            }
        }
        await tx.CommitAsync(ct); return id;
    }
    public async Task<ReturnReceipt> ReceiptAsync(long id, ClaimsPrincipal user, CancellationToken ct = default)
    {
        int actor = await Actor(user, ct);
        var receipt = await db.DocMotDongAsync("""
            SELECT p.id,p.ma_phieu,p.hoa_don_goc_id,h.ma_hoa_don,p.trang_thai,p.tien_hoan,
                p.diem_thu_hoi,p.ngay_lap,t.ho_ten FROM phieu_doi_tra p
            JOIN hoa_don h ON h.id=p.hoa_don_goc_id JOIN tai_khoan t ON t.id=p.thu_ngan_id
            WHERE p.id=@id AND p.loai='TRA_HANG' AND p.thu_ngan_id=@actor
            """, r => new ReturnReceipt(r.SoNguyenDai(0), r.Chuoi(1), r.SoNguyenDai(2), r.Chuoi(3), r.Chuoi(4),
                r.SoNguyenDai(5), r.SoNguyen(6), GioVietNam.ThanhOffset(r.NgayGio(7)), r.Chuoi(8), []), ct, ("@id", id), ("@actor", actor))
            ?? throw LoiNghiepVu.KhongTimThay("Không tìm thấy phiếu trả của bạn.");
        var lines = await db.DocDanhSachAsync("""
            SELECT c.san_pham_id,d.ten_san_pham,c.so_luong,c.don_gia_thuc_tra,c.ly_do,c.ghi_chu_ly_do,
                c.tinh_trang,c.cach_xu_ly,c.ngoai_le,c.gia_von FROM ct_phieu_doi_tra c
            JOIN ct_hoa_don d ON d.hoa_don_id=c.hoa_don_id AND d.san_pham_id=c.san_pham_id
            WHERE c.phieu_doi_tra_id=@id ORDER BY c.san_pham_id
            """, r => new ReturnPreviewLine(r.SoNguyen(0), r.Chuoi(1), r.SoNguyen(2), r.SoNguyenDai(3), r.Chuoi(4),
                r.ChuoiHoacNull(5), r.ChuoiHoacNull(6) ?? "", r.Chuoi(7), r.DungSai(8), Convert.ToDecimal(r[9])), ct, ("@id", id));
        return receipt with { Lines = lines };
    }
}
