using System.Data;
using System.Security.Claims;
using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Modules.Inventory.Models;
using ConvenienceStore.Web.Modules.Inventory.Services;
using ConvenienceStore.Web.Modules.Returns.Models;
using ConvenienceStore.Web.Services.Pos;
using ConvenienceStore.Web.Shared.Data;
using ConvenienceStore.Web.Shared.Errors;
using ConvenienceStore.Web.Shared.Api;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Modules.Returns.Services;

public sealed class ExchangeService(
    CuaHangDbContext db,
    IPosCheckoutStore checkout,
    IDichVuKho stock,
    IReturnPointsGateway points,
    TimeProvider time)
{
    private sealed record Source(
        long Id, string Code, string Status, DateTime PurchasedAt,
        int? CustomerId, int PointsEarned);

    private sealed record SourceLine(
        int ProductId, string Name, string Unit, int Quantity, long Price,
        long Discount, decimal Cost, bool Forbidden, long Reserved);

    private sealed record CartLine(
        long CartId, int ProductId, string Name, string Unit, int Quantity, long Price,
        int? PromotionId, long Discount, string ProductStatus);

    private sealed record Policy(int Id, int Days, long Threshold);

    private DateTime Now => time.GetUtcNow().ToOffset(GioVietNam.DoLech).DateTime;

    public async Task<ExchangePreview> PreviewAsync(
        ExchangeRequest request,
        ClaimsPrincipal user,
        CancellationToken ct = default)
    {
        var actor = await ActorAsync(user, ct);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var preview = await CalculateAsync(request, actor, ct);
        await tx.RollbackAsync(ct);
        return preview;
    }

    public async Task<long> CompleteAsync(
        ExchangeRequest request,
        ClaimsPrincipal user,
        CancellationToken ct = default)
    {
        var actor = await ActorAsync(user, ct);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

        var preview = await CalculateAsync(request, actor, ct);
        if (!preview.CanComplete)
            throw new LoiNghiepVu("DOI_HANG_KHONG_THE_HOAN_TAT", 422, preview.Error ?? "Phiếu đổi chưa đủ điều kiện hoàn tất.");

        var source = await ReadSourceAsync(request.InvoiceId, ct)
            ?? throw LoiNghiepVu.KhongTimThay("Không tìm thấy hóa đơn gốc.");

        var saved = await checkout.SaveOrderAsync(
            actor, request.CashReceived, ct, preview.ReturnValue);

        var period = Now.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
        await db.ChayAsync("""
            INSERT INTO bo_dem_chung_tu(tien_to,ky,so_cuoi)
            VALUES ('TH',@ky,1)
            ON DUPLICATE KEY UPDATE so_cuoi=so_cuoi+1
            """, ct, ("@ky", period));
        var serial = Convert.ToInt64(await db.DocMotGiaTriAsync(
            "SELECT so_cuoi FROM bo_dem_chung_tu WHERE tien_to='TH' AND ky=@ky",
            ct, ("@ky", period)));
        var code = $"TH-{period}-{serial:D3}";

        // Thu hồi điểm trước khi ghi phiếu
        var diemThuHoi = 0;
        if (source.CustomerId is int customer && preview.ReturnedLines.Count > 0)
        {
            diemThuHoi = await points.RecoverAsync(db, source.Id, 0, actor, preview.ReturnValue, ct);
        }

        await db.ChayAsync("""
            INSERT INTO phieu_doi_tra
            (ma_phieu,loai,hoa_don_goc_id,hoa_don_moi_id,khach_hang_id,chinh_sach_id,
             trang_thai,gia_tri_hang_tra,tien_hoan,tien_thu_them,diem_thu_hoi,thu_ngan_id,hoan_tat_luc)
            VALUES
            (@ma,'DOI_HANG',@goc,@moi,@kh,@cs,'HOAN_TAT',@tra,0,@thu,@diem,@tn,@luc)
            """, ct,
            ("@ma", code), ("@goc", source.Id), ("@moi", saved.InvoiceId),
            ("@kh", source.CustomerId), ("@cs", previewPolicyId),
            ("@tra", preview.ReturnValue), ("@thu", preview.Difference),
            ("@diem", diemThuHoi), ("@tn", actor), ("@luc", Now));

        var returnId = Convert.ToInt64(await db.DocMotGiaTriAsync("SELECT LAST_INSERT_ID()", ct));
        foreach (var line in preview.ReturnedLines)
        {
            await db.ChayAsync("""
                INSERT INTO ct_phieu_doi_tra
                (phieu_doi_tra_id,san_pham_id,hoa_don_id,so_luong,don_gia_thuc_tra,
                 ly_do,ghi_chu_ly_do,tinh_trang,cach_xu_ly,gia_von)
                VALUES (@p,@sp,@hd,@sl,@gia,@lydo,@note,@condition,'NHAP_LAI',@cost)
                """, ct,
                ("@p", returnId), ("@sp", line.ProductId), ("@hd", source.Id),
                ("@sl", line.Quantity), ("@gia", line.UnitPaid), ("@lydo", line.Reason),
                ("@note", line.Note), ("@condition", line.Condition), ("@cost", line.Cost));
        }

        var restock = preview.ReturnedLines
            .Select(x => new DongBienDong(x.ProductId, x.Quantity, GiaVon: x.Cost))
            .ToList();
        await stock.GhiNhanBienDongAsync(
            new(LoaiBienDong.TRA_HANG, checked((int)returnId), code, actor, restock), ct);

        if (source.CustomerId is not null && preview.ReturnedLines.Count > 0)
        {
            // Cập nhật returnId vào lich_su_diem
            var recovered = await points.RecoverAsync(db, source.Id, returnId, actor, preview.ReturnValue, ct);
        }

        await tx.CommitAsync(ct);
        return returnId;
    }

    private int previewPolicyId;

    private async Task<ExchangePreview> CalculateAsync(
        ExchangeRequest request, int actor, CancellationToken ct)
    {
        if (request.InvoiceId <= 0 || request.Lines is null || request.Lines.Count == 0)
            throw LoiNghiepVu.DuLieuKhongHopLe("lines", "Chọn ít nhất một dòng hàng trả.");
        if (request.CashReceived < 0)
            throw LoiNghiepVu.DuLieuKhongHopLe("cashReceived", "Tiền khách đưa không hợp lệ.");

        var source = await ReadSourceAsync(request.InvoiceId, ct)
            ?? throw LoiNghiepVu.KhongTimThay("Không tìm thấy hóa đơn gốc.");
        if (source.Status != "DA_THANH_TOAN")
            throw LoiNghiepVu.XungDot("HOA_DON_KHONG_HOP_LE", "Hóa đơn gốc chưa thanh toán hoặc đã hủy.");

        var policy = await db.DocMotDongAsync("""
            SELECT id,so_ngay_doi_tra,nguong_tien_can_duyet
            FROM chinh_sach_doi_tra WHERE hieu_luc_tu<=@now
            ORDER BY hieu_luc_tu DESC,id DESC LIMIT 1
            """, r => new Policy(r.SoNguyen(0), r.SoNguyen(1), r.SoNguyenDai(2)), ct,
            ("@now", Now)) ?? throw LoiNghiepVu.XungDot("CHUA_CO_CHINH_SACH", "Chưa có chính sách đổi trả.");
        previewPolicyId = policy.Id;

        var sourceLines = await ReadSourceLinesAsync(source.Id, ct);
        var returned = new List<ReturnPreviewLine>();
        foreach (var input in request.Lines.Where(x => x.Quantity > 0))
        {
            var line = sourceLines.SingleOrDefault(x => x.ProductId == input.ProductId)
                ?? throw LoiNghiepVu.DuLieuKhongHopLe("productId", "Sản phẩm không thuộc hóa đơn gốc.");
            var remaining = Math.Max(0, line.Quantity - line.Reserved);
            if (input.Quantity > remaining)
                throw LoiNghiepVu.DuLieuKhongHopLe("quantity", $"{line.Name}: chỉ còn {remaining} sản phẩm được trả.");
            if (line.Forbidden)
                return new ExchangePreview(source.Id, 0, 0, 0, [], false, "Dòng hàng không đổi trả; cần Admin duyệt theo DT-06.");
            if (string.IsNullOrWhiteSpace(input.Condition) || input.Condition.Length > 255)
                throw LoiNghiepVu.DuLieuKhongHopLe("condition", "Tình trạng hàng không hợp lệ.");
            var paid = checked(line.Price - line.Discount);
            returned.Add(new ReturnPreviewLine(line.ProductId, line.Name, input.Quantity, paid,
                input.Reason, input.Note?.Trim(), input.Condition.Trim(), "NHAP_LAI", false,
                line.Cost, false, 0));
        }
        if (returned.Count == 0)
            throw LoiNghiepVu.DuLieuKhongHopLe("lines", "Chọn số lượng hàng trả lớn hơn 0.");

        var age = (Now.Date - source.PurchasedAt.Date).TotalDays;
        var returnValue = returned.Sum(x => checked(x.UnitPaid * x.Quantity));
        if (age > policy.Days || returnValue > policy.Threshold)
            return new ExchangePreview(source.Id, returnValue, 0, 0, returned, false, "Phiếu đổi ngoài điều kiện hoặc vượt ngưỡng; cần Admin duyệt theo DT-06.");

        var cart = await ReadCartAsync(actor, ct);
        if (cart.Count == 0)
            return new ExchangePreview(source.Id, returnValue, 0, 0, returned, false, "Hãy thêm hàng mới vào giỏ POS trước khi hoàn tất đổi hàng.");

        // DT-03: Validate khách hàng giỏ POS trùng khách hàng hóa đơn gốc
        var cartCustomer = await db.DocMotGiaTriAsync("""
            SELECT ma_khach_hang FROM gio_hang WHERE id=@id
            """, ct, ("@id", cart[0].CartId));
        if (cartCustomer?.ToString() != source.CustomerId?.ToString())
            return new ExchangePreview(source.Id, returnValue, 0, 0, returned, false, "Giỏ POS không thuộc khách hàng gốc.");

        if (cart.Any(x => x.ProductStatus != "DANG_KINH_DOANH" || x.Quantity <= 0))
            return new ExchangePreview(source.Id, returnValue, 0, 0, returned, false, "Hàng mới không còn kinh doanh.");

        var total = cart.Sum(x => checked(x.Quantity * (x.Price - x.Discount)));
        var difference = checked(total - returnValue);
        if (difference < 0)
            return new ExchangePreview(source.Id, returnValue, total, difference, returned, false, "Giá trị hàng mới thấp hơn hàng trả; hãy thêm hàng hoặc chuyển sang trả hàng.");
        if (request.CashReceived < difference)
            return new ExchangePreview(source.Id, returnValue, total, difference, returned, false, $"Cần thu thêm {difference:N0} đ.");
        return new ExchangePreview(source.Id, returnValue, total, difference, returned, true, null);
    }

    private async Task<int> ActorAsync(ClaimsPrincipal user, CancellationToken ct)
    {
        ReturnLookupAccess.Require(user);
        var id = user.LayTaiKhoanId();
        var found = await db.DocMotGiaTriAsync("""
            SELECT t.id FROM tai_khoan t JOIN vai_tro v ON v.id=t.vai_tro_id
            WHERE t.id=@id AND v.ma='TN' AND t.trang_thai='HOAT_DONG' AND t.bat_buoc_doi_mk=FALSE
            """, ct, ("@id", id));
        if (found is null) throw new LoiNghiepVu("KHONG_DU_QUYEN", 403, "Chỉ thu ngân được đổi hàng.");
        return id;
    }

    private Task<Source?> ReadSourceAsync(long id, CancellationToken ct) => db.DocMotDongAsync("""
        SELECT id,ma_hoa_don,trang_thai,thanh_toan_luc,khach_hang_id,diem_cong
        FROM hoa_don WHERE id=@id FOR UPDATE
        """, r => new Source(r.SoNguyenDai(0), r.Chuoi(1), r.Chuoi(2), r.NgayGio(3),
            r.IsDBNull(4) ? null : r.SoNguyen(4), r.SoNguyen(5)), ct, ("@id", id));

    private Task<List<SourceLine>> ReadSourceLinesAsync(long id, CancellationToken ct) => db.DocDanhSachAsync("""
        SELECT d.san_pham_id,d.ten_san_pham,d.don_vi_tinh,d.so_luong,d.don_gia,d.giam_don_vi,
               d.gia_von,dm.khong_doi_tra,COALESCE((SELECT SUM(c.so_luong) FROM ct_phieu_doi_tra c
               JOIN phieu_doi_tra p ON p.id=c.phieu_doi_tra_id WHERE c.hoa_don_id=d.hoa_don_id
               AND c.san_pham_id=d.san_pham_id AND p.trang_thai IN ('CHO_DUYET','DA_DUYET','HOAN_TAT')),0)
        FROM ct_hoa_don d JOIN san_pham sp ON sp.id=d.san_pham_id JOIN danh_muc dm ON dm.id=sp.danh_muc_id
        WHERE d.hoa_don_id=@id
        """, r => new SourceLine(r.SoNguyen(0), r.Chuoi(1), r.Chuoi(2), r.SoNguyen(3), r.SoNguyenDai(4),
            r.SoNguyenDai(5), Convert.ToDecimal(r[6]), r.DungSai(7), r.SoNguyenDai(8)), ct, ("@id", id));

    private Task<List<CartLine>> ReadCartAsync(int actor, CancellationToken ct) => db.DocDanhSachAsync("""
        SELECT g.id,c.san_pham_id,sp.ten_san_pham,sp.don_vi_tinh,c.so_luong,c.don_gia,c.khuyen_mai_id,c.giam_don_vi,sp.trang_thai
        FROM gio_hang g JOIN ct_gio_hang c ON c.gio_hang_id=g.id JOIN san_pham sp ON sp.id=c.san_pham_id
        WHERE g.thu_ngan_id=@actor AND g.trang_thai='DANG_BAN' ORDER BY c.san_pham_id
        """, r => new CartLine(r.SoNguyenDai(0), r.SoNguyen(1), r.Chuoi(2), r.Chuoi(3), r.SoNguyen(4), r.SoNguyenDai(5),
            r.IsDBNull(6) ? null : r.SoNguyen(6), r.SoNguyenDai(7), r.Chuoi(8)), ct, ("@actor", actor));
}
