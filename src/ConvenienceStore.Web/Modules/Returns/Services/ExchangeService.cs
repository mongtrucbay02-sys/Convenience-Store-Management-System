using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Modules.Inventory.Models;
using ConvenienceStore.Web.Modules.Inventory.Services;
using ConvenienceStore.Web.Modules.Returns.Models;
using ConvenienceStore.Web.Services.Pos;
using ConvenienceStore.Web.Shared.Api;
using ConvenienceStore.Web.Shared.Data;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ConvenienceStore.Web.Modules.Returns.Services;

/// <summary>
/// DT-03. The POS checkout store is reused to create the new invoice from the
/// cashier's active cart. This service owns the transaction that also records
/// the exchange slip, returned stock and points recovery.
/// </summary>
public sealed class ExchangeService(CuaHangDbContext db, IDichVuKho stock,
    IPosCheckoutStore checkoutStore, PosCartService posCart, TimeProvider time, IReturnPointsGateway points)
{
    private sealed record Header(long Id, string Code, string Status, DateTime PaidAt,
        int? CustomerId);
    private sealed record Policy(int Id, int Days, long Threshold);
    private sealed record OriginalLine(int Id, string Name, string Unit, int Purchased,
        long Price, long Discount, decimal Cost, bool Forbidden, long Reserved);
    private sealed record Cart(int Id, int? CustomerId);
    private sealed record CartLine(int Id, string Name, string Unit, int Quantity, long Price,
        int? PromotionId, long Discount, string Status, string? PromotionName);
    private sealed record Draft(Header Invoice, Policy Policy, List<OriginalLine> Original,
        ReturnForm Form, ExchangePreview Preview);

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

    private async Task<Header> ReadInvoice(long id, CancellationToken ct)
    {
        if (id <= 0) throw LoiNghiepVu.DuLieuKhongHopLe("invoiceId", "Mã hóa đơn không hợp lệ.");
        var h = await db.DocMotDongAsync("""
            SELECT id,ma_hoa_don,trang_thai,thanh_toan_luc,khach_hang_id
            FROM hoa_don WHERE id=@id FOR UPDATE
            """, r => new Header(r.SoNguyenDai(0), r.Chuoi(1), r.Chuoi(2), r.NgayGio(3),
                r.IsDBNull(4) ? null : r.SoNguyen(4)), ct, ("@id", id));
        if (h is null) throw LoiNghiepVu.KhongTimThay("Không tìm thấy hóa đơn gốc.");
        if (h.Status != "DA_THANH_TOAN" || h.PaidAt > Now)
            throw new LoiNghiepVu("DOI_TRA_XUNG_DOT", 409, "Hóa đơn gốc không hợp lệ hoặc đã bị hủy.");
        return h;
    }

    private Task<List<OriginalLine>> ReadOriginalLines(long invoiceId, CancellationToken ct) => db.DocDanhSachAsync("""
        SELECT d.san_pham_id,d.ten_san_pham,d.don_vi_tinh,d.so_luong,d.don_gia,d.giam_don_vi,d.gia_von,
               dm.khong_doi_tra,COALESCE((SELECT SUM(c.so_luong) FROM ct_phieu_doi_tra c
                 JOIN phieu_doi_tra p ON p.id=c.phieu_doi_tra_id
                 WHERE c.hoa_don_id=d.hoa_don_id AND c.san_pham_id=d.san_pham_id
                   AND p.trang_thai IN ('CHO_DUYET','DA_DUYET','HOAN_TAT')),0)
        FROM ct_hoa_don d JOIN san_pham sp ON sp.id=d.san_pham_id JOIN danh_muc dm ON dm.id=sp.danh_muc_id
        WHERE d.hoa_don_id=@id ORDER BY d.san_pham_id
        """, r => new OriginalLine(r.SoNguyen(0), r.Chuoi(1), r.Chuoi(2), r.SoNguyen(3),
            r.SoNguyenDai(4), r.SoNguyenDai(5), Convert.ToDecimal(r[6]), r.DungSai(7), r.SoNguyenDai(8)), ct, ("@id", invoiceId));

    private async Task<(Cart Cart, List<CartLine> Lines)> ReadActiveCart(int cashier, CancellationToken ct)
    {
        var cart = await db.DocMotDongAsync("""
            SELECT id,khach_hang_id FROM gio_hang WHERE thu_ngan_id=@t AND trang_thai='DANG_BAN' FOR UPDATE
            """, r => new Cart(r.SoNguyen(0), r.IsDBNull(1) ? null : r.SoNguyen(1)), ct, ("@t", cashier));
        if (cart is null) throw new LoiNghiepVu("DT03_CART_MISSING", 409, "Thu ngân chưa có giỏ hàng đang bán. Hãy mở POS và thêm hàng mới.");
        var lines = await db.DocDanhSachAsync("""
            SELECT ct.san_pham_id,sp.ten_san_pham,sp.don_vi_tinh,ct.so_luong,ct.don_gia,
                   ct.khuyen_mai_id,ct.giam_don_vi,sp.trang_thai,km.ten
            FROM ct_gio_hang ct JOIN san_pham sp ON sp.id=ct.san_pham_id
            LEFT JOIN chuong_trinh_khuyen_mai km ON km.id=ct.khuyen_mai_id
            WHERE ct.gio_hang_id=@g ORDER BY ct.san_pham_id
            """, r => new CartLine(r.SoNguyen(0), r.Chuoi(1), r.Chuoi(2), r.SoNguyen(3), r.SoNguyenDai(4),
                r.IsDBNull(5) ? null : r.SoNguyen(5), r.SoNguyenDai(6), r.Chuoi(7), r.ChuoiHoacNull(8)), ct, ("@g", cart.Id));
        return (cart, lines);
    }

    public async Task<ExchangePage> FormAsync(long invoiceId, ClaimsPrincipal user, CancellationToken ct = default)
    {
        await Actor(user, ct);
        // POS cart service creates the cashier's active cart if none exists and refreshes promotions.
        await posCart.GetCartAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var h = await ReadInvoice(invoiceId, ct);
        var lines = await ReadOriginalLines(invoiceId, ct);
        var form = new ReturnForm(h.Id, h.Code, GioVietNam.ThanhOffset(h.PaidAt), lines.Select(l =>
            new ReturnFormLine(l.Id, l.Name, l.Unit, l.Price - l.Discount, l.Purchased,
                Math.Max(0, l.Purchased - l.Reserved), l.Forbidden)).ToList());
        var cart = await ReadActiveCart(user.LayTaiKhoanId(), ct);
        await tx.CommitAsync(ct);
        return new ExchangePage { Invoice = form, Request = new ExchangeRequest { InvoiceId = invoiceId }, NewItems = ToCartPreview(cart.Lines) };
    }

    private static List<ExchangeCartLine> ToCartPreview(IEnumerable<CartLine> lines) => lines.Select(x =>
        new ExchangeCartLine(x.Id, x.Name, x.Unit, x.Quantity, x.Price, x.Discount,
            x.PromotionName ?? "", checked((long)x.Quantity * (x.Price - x.Discount)))).ToList();

    private async Task<Draft> Calculate(ExchangeRequest request, int actor, CancellationToken ct)
    {
        if (request.CashReceived < 0)
            throw LoiNghiepVu.DuLieuKhongHopLe("cashReceived", "Tiền khách đưa không được âm.");
        if (request.Lines is null || request.Lines.Count is 0 or > 100 || request.Lines.Any(x => x is null))
            throw LoiNghiepVu.DuLieuKhongHopLe("lines", "Chọn tối thiểu một dòng hàng trả.");
        var selected = request.Lines.Where(x => x.Quantity > 0).OrderBy(x => x.ProductId).ToList();
        if (selected.Count == 0 || selected.Any(x => x.ProductId <= 0) || selected.Select(x => x.ProductId).Distinct().Count() != selected.Count)
            throw LoiNghiepVu.DuLieuKhongHopLe("lines", "Số lượng trả phải dương và không lặp sản phẩm.");
        foreach (var l in selected)
        {
            if (!Reasons.Contains(l.Reason ?? "")) throw LoiNghiepVu.DuLieuKhongHopLe("reason", "Chọn lý do đổi hợp lệ.");
            if (!Handlings.Contains(l.Handling ?? "")) throw LoiNghiepVu.DuLieuKhongHopLe("handling", "Chọn nhập lại kho hoặc hủy hàng.");
            if (string.IsNullOrWhiteSpace(l.Condition) || l.Condition.Length > 255)
                throw LoiNghiepVu.DuLieuKhongHopLe("condition", "Nhập tình trạng hàng, tối đa 255 ký tự.");
            if (l.Note?.Length > 255 || (l.Reason == "KHAC" && string.IsNullOrWhiteSpace(l.Note)))
                throw LoiNghiepVu.DuLieuKhongHopLe("note", "Lý do khác cần ghi chú, tối đa 255 ký tự.");
        }

        var h = await ReadInvoice(request.InvoiceId, ct);
        var original = await ReadOriginalLines(h.Id, ct);
        var now = Now;
        var policy = await db.DocMotDongAsync("""
            SELECT id,so_ngay_doi_tra,nguong_tien_can_duyet FROM chinh_sach_doi_tra
            WHERE hieu_luc_tu<=@now ORDER BY hieu_luc_tu DESC,id DESC LIMIT 1 FOR SHARE
            """, r => new Policy(r.SoNguyen(0), r.SoNguyen(1), r.SoNguyenDai(2)), ct, ("@now", now))
            ?? throw new LoiNghiepVu("DOI_TRA_XUNG_DOT", 409, "Chưa có chính sách đổi trả đang hiệu lực.");
        bool expired = (now.Date - h.PaidAt.Date).TotalDays > policy.Days;
        var returnPreview = new List<ExchangePreviewLine>();
        long returnValue = 0;
        foreach (var l in selected)
        {
            var saved = original.SingleOrDefault(x => x.Id == l.ProductId);
            if (saved is null) throw LoiNghiepVu.DuLieuKhongHopLe("productId", "Sản phẩm không thuộc hóa đơn gốc.");
            long remaining = Math.Max(0, (long)saved.Purchased - saved.Reserved);
            if (l.Quantity > remaining) throw LoiNghiepVu.DuLieuKhongHopLe("quantity", $"{saved.Name}: chỉ còn {remaining} sản phẩm được đổi.");
            if (saved.Price < saved.Discount) throw new LoiNghiepVu("DOI_TRA_XUNG_DOT", 409, "Giá thực trả trên hóa đơn không hợp lệ.");
            long paid = saved.Price - saved.Discount;
            returnValue = checked(returnValue + paid * l.Quantity);
            returnPreview.Add(new(saved.Id, saved.Name, l.Quantity, paid, l.Reason, l.Note?.Trim(), l.Condition.Trim(), l.Handling,
                expired || saved.Forbidden, saved.Cost));
        }
        bool forbidden = returnPreview.Any(x => original.Single(o => o.Id == x.ProductId).Forbidden);
        bool overThreshold = returnValue > policy.Threshold;
        bool needsApproval = expired || forbidden || overThreshold;

        var activeCart = await ReadActiveCart(actor, ct);
        if (activeCart.Cart.CustomerId != h.CustomerId)
            throw new LoiNghiepVu("DT03_CUSTOMER_MISMATCH", 409, "Khách hàng trong giỏ POS phải trùng khách hàng của hóa đơn gốc.");
        if (activeCart.Lines.Count == 0) throw new LoiNghiepVu("DT03_CART_EMPTY", 422, "Hãy thêm hàng mới vào giỏ POS trước khi đổi hàng.");
        var promotions = await PosPromotionCalculator.ReadAsync(db.Database.GetDbConnection(), db.Database.CurrentTransaction!.GetDbTransaction(), ct);
        long newTotal = 0;
        foreach (var line in activeCart.Lines)
        {
            if (line.Status != "DANG_KINH_DOANH" || line.Quantity <= 0 || line.Price <= 0 || line.Price % 100 != 0)
                throw new LoiNghiepVu("DT03_CART_INVALID", 409, $"Sản phẩm {line.Name} không còn bán hoặc giá/số lượng không hợp lệ.");
            var best = PosPromotionCalculator.Best(promotions, line.Id, line.Price, activeCart.Cart.CustomerId is not null);
            var discount = best is null ? 0 : PosPromotionCalculator.Discount(line.Price, best);
            if (line.PromotionId != best?.Id || line.Discount != discount)
                throw new LoiNghiepVu("DT03_PROMOTION_CHANGED", 409, "Khuyến mãi giỏ POS đã thay đổi. Hãy mở POS, kiểm tra và xác nhận lại giỏ hàng.");
            newTotal = checked(newTotal + (long)line.Quantity * (line.Price - line.Discount));
        }
        if (newTotal <= 0) throw LoiNghiepVu.DuLieuKhongHopLe("cart", "Giá trị hàng mới phải lớn hơn 0.");
        if (returnValue > newTotal) throw new LoiNghiepVu("DT03_NEGATIVE_DIFFERENCE", 422, "Giá trị hàng đổi thấp hơn hàng trả. Chọn thêm hàng mới hoặc chuyển sang DT-02 trả hàng.");
        long cashDue = newTotal - returnValue;
        long change = checked(request.CashReceived - cashDue);
        var cartPreview = ToCartPreview(activeCart.Lines);
        var tokenPayload = new { actor, h, policy, expired, original, returnPreview, returnValue, newTotal, cashDue, activeCart.Cart, cartPreview };
        string token = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(tokenPayload))));
        return new(h, policy, original, new ReturnForm(h.Id, h.Code, GioVietNam.ThanhOffset(h.PaidAt), original.Select(l =>
            new ReturnFormLine(l.Id, l.Name, l.Unit, l.Price - l.Discount, l.Purchased, Math.Max(0, l.Purchased - l.Reserved), l.Forbidden)).ToList()),
            new ExchangePreview(h.Id, h.Code, returnValue, newTotal, returnValue, cashDue, request.CashReceived,
                Math.Max(0, change), expired, forbidden, overThreshold, needsApproval, token, returnPreview, cartPreview));
    }

    public async Task<ExchangePreview> PreviewAsync(ExchangeRequest request, ClaimsPrincipal user, CancellationToken ct = default)
    {
        int actor = await Actor(user, ct);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var draft = await Calculate(request, actor, ct);
        await tx.CommitAsync(ct);
        return draft.Preview;
    }

    public async Task<long> CreateAsync(ExchangeRequest request, ClaimsPrincipal user, CancellationToken ct = default)
    {
        int actor = await Actor(user, ct);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var draft = await Calculate(request, actor, ct);
        var preview = draft.Preview;
        if (string.IsNullOrWhiteSpace(request.ConfirmationToken) || request.ConfirmationToken != preview.ConfirmationToken)
            throw new LoiNghiepVu("DOI_TRA_XUNG_DOT", 409, "Giỏ hàng, giá, khuyến mãi hoặc số lượng đổi đã thay đổi. Vui lòng tính lại.");
        if (!preview.NeedsApproval && request.CashReceived < preview.CashDue)
            throw LoiNghiepVu.DuLieuKhongHopLe("cashReceived", $"Khách cần trả thêm {preview.CashDue:N0} đ.");
        if (!preview.NeedsApproval && preview.CashDue > 0 && !request.CashConfirmed)
            throw LoiNghiepVu.DuLieuKhongHopLe("cashConfirmed", "Cần xác nhận thu tiền chênh lệch.");

        string period = Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        await db.ChayAsync($"INSERT INTO bo_dem_chung_tu (tien_to,ky,so_cuoi) VALUES ('TH',@ky,1) {db.CounterUpsert()}", ct, ("@ky", period));
        long serial = Convert.ToInt64(await db.DocMotGiaTriAsync("SELECT so_cuoi FROM bo_dem_chung_tu WHERE tien_to='TH' AND ky=@ky", ct, ("@ky", period)));
        string code = $"DT-{period}-{serial:D3}";
        if (code.Length > 20) throw new LoiNghiepVu("DOI_TRA_XUNG_DOT", 409, "Mã phiếu đổi vượt giới hạn.");

        PosOrderSaved? newInvoice = null;
        string status = preview.NeedsApproval ? "CHO_DUYET" : "HOAN_TAT";
        if (!preview.NeedsApproval)
        {
            // POS-07 save runs inside this transaction. Exchange credit is the returned value.
            try
            {
                newInvoice = await checkoutStore.SaveOrderAsync(actor, preview.CashDue > 0 ? request.CashReceived : 0, ct, preview.ExchangeCredit);
            }
            catch (PosCheckoutException ex)
            {
                throw new LoiNghiepVu("DOI_TRA_XUNG_DOT", 409, ex.Message, loiGoc: ex);
            }
        }

        long exchangeId = await db.InsertIdAsync("""
            INSERT INTO phieu_doi_tra (ma_phieu,loai,hoa_don_goc_id,hoa_don_moi_id,khach_hang_id,chinh_sach_id,
                trang_thai,can_duyet_qua_han,can_duyet_danh_muc,can_duyet_vuot_nguong,gia_tri_hang_tra,tien_hoan,tien_thu_them,
                thu_ngan_id,hoan_tat_luc)
            VALUES (@ma,'DOI_HANG',@goc,@moi,@kh,@cs,@status,@expired,@forbidden,@over,@return,0,@extra,@actor,@done)
            """, ct, ("@ma", code), ("@goc", draft.Invoice.Id), ("@moi", newInvoice?.InvoiceId), ("@kh", draft.Invoice.CustomerId),
            ("@cs", draft.Policy.Id), ("@status", status), ("@expired", preview.Expired), ("@forbidden", preview.Forbidden),
            ("@over", preview.OverThreshold), ("@return", preview.ReturnValue), ("@extra", preview.CashDue),
            ("@actor", actor), ("@done", preview.NeedsApproval ? null : Now));
        foreach (var l in preview.ReturnLines)
            await db.ChayAsync("""
                INSERT INTO ct_phieu_doi_tra (phieu_doi_tra_id,san_pham_id,hoa_don_id,so_luong,don_gia_thuc_tra,
                    ly_do,ghi_chu_ly_do,tinh_trang,cach_xu_ly,ngoai_le,gia_von)
                VALUES (@p,@sp,@goc,@sl,@price,@reason,@note,@condition,@handling,@exception,@cost)
                """, ct, ("@p", exchangeId), ("@sp", l.ProductId), ("@goc", draft.Invoice.Id), ("@sl", l.Quantity),
                ("@price", l.UnitPaid), ("@reason", l.Reason), ("@note", l.Note), ("@condition", l.Condition),
                ("@handling", l.Handling), ("@exception", l.Exception), ("@cost", l.Cost));

        if (!preview.NeedsApproval && newInvoice is not null)
        {
            var restock = preview.ReturnLines.Where(l => l.Handling == "NHAP_LAI")
                .Select(l => new DongBienDong(l.ProductId, SoLuong: l.Quantity, GiaVon: l.Cost)).ToList();
            if (restock.Count > 0)
                await stock.GhiNhanBienDongAsync(new(LoaiBienDong.TRA_HANG, checked((int)exchangeId), code, actor, restock), ct);

            int pointsRecovered = await points.RecoverAsync(db, draft.Invoice.Id, exchangeId, actor, preview.ReturnValue, ct);
            await db.ChayAsync("UPDATE phieu_doi_tra SET diem_thu_hoi=@d WHERE id=@id", ct, ("@d", pointsRecovered), ("@id", exchangeId));
        }
        await tx.CommitAsync(ct);
        return exchangeId;
    }

    public async Task<ExchangeReceipt> ReceiptAsync(long id, ClaimsPrincipal user, CancellationToken ct = default)
    {
        int actor = await Actor(user, ct);
        var h = await db.DocMotDongAsync("""
            SELECT p.id,p.ma_phieu,p.hoa_don_goc_id,h.ma_hoa_don,p.hoa_don_moi_id,hm.ma_hoa_don,p.trang_thai,
                   p.gia_tri_hang_tra,p.tien_thu_them,p.diem_thu_hoi,p.ngay_lap,t.ho_ten,
                   COALESCE(hm.tong_thanh_toan,0),COALESCE(hm.tien_tru_hang_doi,0),COALESCE(hm.tien_khach_dua,0),COALESCE(hm.tien_thua,0),COALESCE(hm.diem_cong,0)
            FROM phieu_doi_tra p JOIN hoa_don h ON h.id=p.hoa_don_goc_id LEFT JOIN hoa_don hm ON hm.id=p.hoa_don_moi_id
            JOIN tai_khoan t ON t.id=p.thu_ngan_id WHERE p.id=@id AND p.loai='DOI_HANG' AND p.thu_ngan_id=@actor
            """, r => new ExchangeReceipt(r.SoNguyenDai(0), r.Chuoi(1), r.SoNguyenDai(2), r.Chuoi(3),
                r.IsDBNull(4) ? 0 : r.SoNguyenDai(4), r.ChuoiHoacNull(5) ?? "", r.Chuoi(6), r.SoNguyenDai(7),
                r.SoNguyenDai(12), r.SoNguyenDai(13), r.SoNguyenDai(14), r.SoNguyenDai(8), r.SoNguyenDai(15),
                r.SoNguyen(9), r.SoNguyen(16), GioVietNam.ThanhOffset(r.NgayGio(10)), r.Chuoi(11), []), ct, ("@id", id), ("@actor", actor))
            ?? throw LoiNghiepVu.KhongTimThay("Không tìm thấy phiếu đổi hàng của bạn.");
        var lines = await db.DocDanhSachAsync("""
            SELECT c.san_pham_id,d.ten_san_pham,c.so_luong,c.don_gia_thuc_tra,c.ly_do,c.ghi_chu_ly_do,
                   c.tinh_trang,c.cach_xu_ly,c.ngoai_le,c.gia_von
            FROM ct_phieu_doi_tra c JOIN ct_hoa_don d ON d.hoa_don_id=c.hoa_don_id AND d.san_pham_id=c.san_pham_id
            WHERE c.phieu_doi_tra_id=@id ORDER BY c.san_pham_id
            """, r => new ExchangePreviewLine(r.SoNguyen(0), r.Chuoi(1), r.SoNguyen(2), r.SoNguyenDai(3), r.Chuoi(4),
                r.ChuoiHoacNull(5), r.ChuoiHoacNull(6) ?? "", r.Chuoi(7), r.DungSai(8), Convert.ToDecimal(r[9])), ct, ("@id", id));
        return h with { ReturnLines = lines };
    }
}
