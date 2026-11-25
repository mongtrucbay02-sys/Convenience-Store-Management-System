using System.Data;
using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
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
using Npgsql;

namespace ConvenienceStore.Web.Modules.Returns.Services;

public sealed class ExchangeService(
    CuaHangDbContext db, ReturnOrderService returns, PostgreSqlPosCheckoutStore checkout,
    IDichVuKho stock, IReturnPointsGateway points, TimeProvider time)
{
    private sealed record Cart(long Id, long? CustomerId, long Version);
    private sealed record CartItem(ExchangeCartLine Line, string Status, int Available, long? PromotionId);

    public async Task<ExchangePage> FormAsync(ExchangeRequest request, ClaimsPrincipal user,
        CancellationToken ct = default)
    {
        var invoice = await returns.FormAsync(request.InvoiceId, user, ct);
        var cart = await ReadCartAsync(user.LayTaiKhoanId(), ct, lockCart: false);
        return new(invoice, request, cart.Items.Select(x => x.Line).ToList());
    }

    public async Task<ExchangePreview> PreviewAsync(ExchangeRequest request, ClaimsPrincipal user,
        CancellationToken ct = default)
    {
        var actor = await returns.Actor(user, ct);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var draft = await CalculateAsync(request, actor, ct);
        await tx.CommitAsync(ct);
        return draft.Preview;
    }

    public async Task<long> CreateAsync(ExchangeRequest request, ClaimsPrincipal user,
        CancellationToken ct = default)
    {
        var actor = await returns.Actor(user, ct);
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            var (customerId, policyId, preview) = await CalculateAsync(request, actor, ct);
            if (!string.Equals(request.ConfirmationToken, preview.ConfirmationToken, StringComparison.Ordinal))
                throw Conflict("Hóa đơn hoặc giỏ POS đã thay đổi. Vui lòng tính lại chênh lệch.");

            if (request.CashReceived < preview.CashDue || (preview.CashDue > 0 && !request.CashConfirmed))
                throw LoiNghiepVu.DuLieuKhongHopLe("cashReceived", $"Xác nhận đã thu đủ {preview.CashDue:N0} đ.");

            var now = time.GetUtcNow();
            var period = now.ToOffset(GioVietNam.DoLech).ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            var serial = Convert.ToInt64(await db.DocMotGiaTriAsync($"""
                INSERT INTO bo_dem_chung_tu(tien_to, ky, so_cuoi)
                VALUES ('TH', @period, 1) {db.CounterUpsert()}
                RETURNING so_cuoi
                """, ct, ("@period", period)));
            var code = $"DT-{period}-{serial:D3}";
            if (code.Length > 20)
                throw Conflict("Mã phiếu đổi vượt giới hạn.");

            // POS ghi hóa đơn, trừ kho hàng mới và cập nhật quỹ trong giao dịch này.
            var order = await checkout.SaveOrderAsync(actor,
                preview.CashDue > 0 ? request.CashReceived : 0, ct, preview.Returned.Refund);
            var id = await db.InsertIdAsync("""
                INSERT INTO phieu_doi_tra(ma_phieu, loai, hoa_don_goc_id, hoa_don_moi_id, khach_hang_id,
                    chinh_sach_id, trang_thai, gia_tri_hang_tra, tien_hoan, tien_thu_them, thu_ngan_id, hoan_tat_luc)
                VALUES (@code, 'DOI_HANG', @invoice, @newInvoice, @kh, @policy, 'HOAN_TAT', @value, 0, @cash, @actor, @now)
                """, ct, ("@code", code), ("@invoice", request.InvoiceId), ("@newInvoice", order.InvoiceId),
                ("@kh", customerId), ("@policy", policyId), ("@value", preview.Returned.Refund),
                ("@cash", preview.CashDue), ("@actor", actor), ("@now", now));

            foreach (var line in preview.Returned.Lines)
            {
                await db.ChayAsync("""
                    INSERT INTO ct_phieu_doi_tra(phieu_doi_tra_id, san_pham_id, hoa_don_id, so_luong,
                        don_gia_thuc_tra, ly_do, ghi_chu_ly_do, tinh_trang, cach_xu_ly, ngoai_le, gia_von)
                    VALUES (@id, @product, @invoice, @qty, @price, @reason, @note, @condition, @handling, FALSE, @cost)
                    """, ct, ("@id", id), ("@product", line.ProductId), ("@invoice", request.InvoiceId),
                    ("@qty", line.Quantity), ("@price", line.UnitPaid), ("@reason", line.Reason),
                    ("@note", line.Note), ("@condition", line.Condition), ("@handling", line.Handling), ("@cost", line.Cost));
            }

            var restock = preview.Returned.Lines.Where(x => x.Handling == "NHAP_LAI")
                .Select(x => new DongBienDong(x.ProductId, SoLuong: x.Quantity, GiaVon: x.Cost)).ToList();
            if (restock.Count > 0)
                await stock.GhiNhanBienDongAsync(new(LoaiBienDong.TRA_HANG, id, code, actor, restock), ct);

            if (preview.Returned.RequiresPointsRecovery)
            {
                var recovered = await points.RecoverAsync(db, request.InvoiceId, id, actor, preview.Returned.Refund, ct);
                await db.ChayAsync("UPDATE phieu_doi_tra SET diem_thu_hoi = @points WHERE id = @id",
                    ct, ("@points", recovered), ("@id", id));
            }

            await tx.CommitAsync(ct);
            return id;
        }
        catch (PosCheckoutException ex)
        {
            throw Conflict(ex.Message);
        }
        catch (PostgresException ex) when (ex.SqlState is PostgresErrorCodes.DeadlockDetected
            or PostgresErrorCodes.SerializationFailure)
        {
            throw Conflict("Dữ liệu đang được giao dịch khác xử lý. Vui lòng tính lại chênh lệch.");
        }
    }

    private async Task<(long? CustomerId, long PolicyId, ExchangePreview Preview)> CalculateAsync(
        ExchangeRequest request, long actor, CancellationToken ct)
    {
        if (request.CashReceived < 0)
            throw LoiNghiepVu.DuLieuKhongHopLe("cashReceived", "Tiền khách đưa không được âm.");

        var (customerId, policyId, returned) = await returns.CalculateForExchangeAsync(request, actor, ct);
        if (returned.NeedsApproval)
            throw Conflict("Phiếu đổi thuộc diện cần Admin duyệt. Vui lòng liên hệ quản lý.");

        var (cart, items) = await ReadCartAsync(actor, ct, lockCart: true);
        if (cart is null || items.Count == 0)
            throw LoiNghiepVu.DuLieuKhongHopLe("cart", "Hãy thêm hàng mới vào giỏ POS trước khi đổi hàng.");
        if (cart.CustomerId != customerId)
            throw Conflict("Khách hàng trong giỏ POS phải trùng với khách hàng của hóa đơn gốc.");

        foreach (var item in items)
        {
            if (item.Status != "DANG_KINH_DOANH")
                throw Conflict($"{item.Line.Name} đã ngừng kinh doanh.");
            if (item.Available < item.Line.Quantity)
                throw Conflict($"{item.Line.Name} không đủ tồn kho. Hãy cập nhật giỏ POS.");
        }

        var newTotal = items.Sum(x => x.Line.LineTotal);
        if (newTotal < returned.Refund)
            throw new LoiNghiepVu("DT03_NEGATIVE_DIFFERENCE", 422,
                "Giá trị hàng đổi thấp hơn hàng trả. Chọn thêm hàng mới hoặc chuyển sang DT-02 trả hàng.");
        if (newTotal <= 0)
            throw LoiNghiepVu.DuLieuKhongHopLe("cart", "Giá trị hàng mới phải lớn hơn 0.");

        var token = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new
        {
            ReturnToken = returned.ConfirmationToken,
            cart,
            Items = items.Select(x => new { x.Line, x.PromotionId })
        })));
        return (customerId, policyId, new(returned, newTotal, newTotal - returned.Refund, token,
            items.Select(x => x.Line).ToList()));
    }

    private async Task<(Cart? Cart, List<CartItem> Items)> ReadCartAsync(long actor, CancellationToken ct, bool lockCart)
    {
        const string activeCart = """
            SELECT id, khach_hang_id, phien_ban FROM gio_hang
            WHERE thu_ngan_id = @actor AND trang_thai = 'DANG_BAN'
            """;
        var cart = await db.DocMotDongAsync(lockCart ? activeCart + " FOR UPDATE" : activeCart,
            r => new Cart(r.SoNguyenDai(0), r.SoNguyenDaiHoacNull(1), r.SoNguyenDai(2)), ct, ("@actor", actor));
        if (cart is null) return (null, []);

        var items = await db.DocDanhSachAsync("""
            SELECT d.san_pham_id, p.ten_san_pham, d.so_luong, d.don_gia, d.giam_don_vi,
                p.trang_thai, COALESCE(k.so_luong_ton, 0), d.khuyen_mai_id
            FROM ct_gio_hang d JOIN san_pham p ON p.id = d.san_pham_id
            LEFT JOIN ton_kho k ON k.san_pham_id = p.id
            WHERE d.gio_hang_id = @id ORDER BY d.san_pham_id
            """, r => new CartItem(new(r.SoNguyenDai(0), r.Chuoi(1), r.SoNguyen(2), r.SoNguyenDai(3),
                r.SoNguyenDai(4)), r.Chuoi(5), r.SoNguyen(6), r.SoNguyenDaiHoacNull(7)), ct, ("@id", cart.Id));
        return (cart, items);
    }

    public async Task<ExchangeReceipt> ReceiptAsync(long id, ClaimsPrincipal user, CancellationToken ct = default)
    {
        var actor = await returns.Actor(user, ct);
        var receipt = await db.DocMotDongAsync("""
            SELECT p.id, p.ma_phieu, old.ma_hoa_don, new.id, new.ma_hoa_don,
                p.gia_tri_hang_tra, new.tong_thanh_toan, p.tien_thu_them, new.tien_khach_dua, new.tien_thua
            FROM phieu_doi_tra p JOIN hoa_don old ON old.id = p.hoa_don_goc_id
            JOIN hoa_don new ON new.id = p.hoa_don_moi_id
            WHERE p.id = @id AND p.loai = 'DOI_HANG' AND p.trang_thai = 'HOAN_TAT' AND p.thu_ngan_id = @actor
            """, r => new ExchangeReceipt(r.SoNguyenDai(0), r.Chuoi(1), r.Chuoi(2), r.SoNguyenDai(3),
                r.Chuoi(4), r.SoNguyenDai(5), r.SoNguyenDai(6), r.SoNguyenDai(7),
                r.SoNguyenDai(8), r.SoNguyenDai(9), []), ct, ("@id", id), ("@actor", actor))
            ?? throw LoiNghiepVu.KhongTimThay("Không tìm thấy phiếu đổi hàng của bạn.");

        var lines = await db.DocDanhSachAsync("""
            SELECT c.san_pham_id, d.ten_san_pham, c.so_luong, c.don_gia_thuc_tra, c.ly_do, c.ghi_chu_ly_do,
                c.tinh_trang, c.cach_xu_ly, c.ngoai_le, c.gia_von, c.gia_tri_huy
            FROM ct_phieu_doi_tra c JOIN ct_hoa_don d ON d.hoa_don_id = c.hoa_don_id AND d.san_pham_id = c.san_pham_id
            WHERE c.phieu_doi_tra_id = @id ORDER BY c.san_pham_id
            """, r => new ReturnPreviewLine(r.SoNguyenDai(0), r.Chuoi(1), r.SoNguyen(2), r.SoNguyenDai(3), r.Chuoi(4),
                r.ChuoiHoacNull(5), r.Chuoi(6), r.Chuoi(7), r.DungSai(8), r.SoThapPhan(9),
                ScrapValue: r.SoThapPhan(10)), ct, ("@id", id));
        return receipt with { ReturnLines = lines };
    }

    private static LoiNghiepVu Conflict(string message) => new("DOI_TRA_XUNG_DOT", 409, message);
}
