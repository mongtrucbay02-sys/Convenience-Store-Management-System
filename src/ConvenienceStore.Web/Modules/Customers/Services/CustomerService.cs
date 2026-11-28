using ConvenienceStore.Web.Modules.Customers.Models;
using System.Data;
using System.Data.Common;
using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Data.Entities;
using ConvenienceStore.Web.Shared.Data;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Modules.Customers.Services;

public sealed class CustomerService(CuaHangDbContext db)
{
    public async Task<List<CustomerRow>> SearchAsync(string? search, bool admin, CancellationToken ct)
    {
        search = search?.Trim() ?? "";
        var query = db.KhachHangs.AsNoTracking().Where(customer => customer.TrangThai == "HOAT_DONG");
        if (search.Length != 0)
        {
            var pattern = "%" + search + "%";
            long? code = long.TryParse(search, out var parsed) && parsed.ToString() == search
                ? parsed : null;
            {
                pattern=VietnameseSearch.Fold(pattern);
                query=query.Where(customer=>EF.Functions.Like(VietnameseSearch.Fold(customer.HoTen!),pattern)
                    || EF.Functions.Like(customer.SoDienThoai!,pattern) || customer.Id==code);
            }
        }

        var rows = await query.OrderByDescending(customer => customer.Id).Take(100)
            .Select(customer => new
            {
                customer.Id,
                customer.HoTen,
                customer.SoDienThoai,
                customer.DiemHienCo,
                Spending = customer.HoaDons.Where(invoice => invoice.TrangThai == "DA_THANH_TOAN")
                    .Sum(invoice => (decimal)invoice.TongThanhToan),
                LastPurchase = customer.HoaDons.Where(invoice => invoice.TrangThai == "DA_THANH_TOAN")
                    .Max(invoice => (DateTime?)invoice.ThanhToanLuc),
                customer.PhienBan
            }).ToListAsync(ct);
        return rows.Select(row => new CustomerRow(row.Id, row.HoTen!,
            admin ? row.SoDienThoai! : CustomerRules.MaskPhone(row.SoDienThoai),
            row.DiemHienCo, (long)row.Spending, row.LastPurchase.HasValue ? ConvenienceStore.Web.Shared.Api.GioVietNam.ThanhOffset(row.LastPurchase.Value).DateTime : null, row.PhienBan)).ToList();
    }

    public async Task<CustomerInput> GetInputAsync(long id, CancellationToken ct)
    {
        var k = await db.KhachHangs.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.TrangThai == "HOAT_DONG", ct)
            ?? throw LoiNghiepVu.KhongTimThay("Không tìm thấy khách hàng.");
        return new CustomerInput { Name = k.HoTen!, Phone = k.SoDienThoai!, Email = k.Email,
            BirthDate = k.NgaySinh?.ToDateTime(TimeOnly.MinValue), Gender = k.GioiTinh, Version = k.PhienBan };
    }

    public async Task<long> SaveAsync(long? id, CustomerInput input, long actor, CancellationToken ct)
    {
        Validate(input);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        KhachHang customer;
        if (id is long customerId)
        {
            await LockAsync(customerId, ct);
            var tracked = db.KhachHangs.Local.SingleOrDefault(row => row.Id == customerId);
            if (tracked is not null)
                await db.Entry(tracked).ReloadAsync(ct);
            customer = tracked ?? await db.KhachHangs.SingleAsync(row => row.Id == customerId, ct);
            if (customer.PhienBan != input.Version)
                throw LoiNghiepVu.XungDot(MaLoiChung.XungDotDuLieu, "Khách đã thay đổi. Hãy tải lại trước khi sửa.");
        }
        else
        {
            customer = new KhachHang { NguoiTaoId = checked((long)actor), TrangThai = "HOAT_DONG" };
        }
        var duplicate = await db.KhachHangs.AsNoTracking()
            .Where(customer => customer.SoDienThoai == input.Phone && customer.Id != id)
            .Select(customer => (long?)customer.Id)
            .FirstOrDefaultAsync(ct);
        if (duplicate.HasValue)
            throw LoiNghiepVu.XungDot(MaLoiChung.TrungDuLieu,
                "Số điện thoại đã tồn tại. Hãy chọn khách đã có.", new { customerId = duplicate.Value });
        customer.HoTen = input.Name.Trim();
        customer.SoDienThoai = input.Phone;
        customer.Email = string.IsNullOrWhiteSpace(input.Email) ? null : input.Email.Trim();
        customer.NgaySinh = input.BirthDate.HasValue ? DateOnly.FromDateTime(input.BirthDate.Value) : null;
        customer.GioiTinh = string.IsNullOrEmpty(input.Gender) ? null : input.Gender;
        if (id.HasValue)
            customer.PhienBan++;
        else
            db.KhachHangs.Add(customer);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is DbException error
            && TruyVanCsdl.LaLoiTrungKhoa(error, "uq_khach_hang_sdt"))
        {
            db.Entry(customer).State = EntityState.Detached;
            throw LoiNghiepVu.XungDot(MaLoiChung.TrungDuLieu, "Số điện thoại đã tồn tại. Hãy tìm lại khách.");
        }
        await tx.CommitAsync(ct);
        return customer.Id;
    }

    public async Task<CustomerHistory> HistoryAsync(long id, bool admin, CancellationToken ct)
    {
        var input = await GetInputAsync(id, ct);
        var invoices = await db.HoaDons
            .AsNoTracking()
            .Where(invoice => invoice.KhachHangId == id)
            .OrderByDescending(invoice => invoice.ThanhToanLuc)
            .ThenByDescending(invoice => invoice.Id)
            .Select(invoice => new CustomerInvoice(invoice.Id, invoice.MaHoaDon,
                invoice.ThanhToanLuc, invoice.TongThanhToan, invoice.DiemCong))
            .ToListAsync(ct);
        var points = await db.LichSuDiems
            .AsNoTracking()
            .Where(entry => entry.KhachHangId == id)
            .OrderByDescending(entry => entry.ThoiDiem)
            .ThenByDescending(entry => entry.Id)
            .Select(entry => new CustomerPoint(entry.Id, entry.Loai, entry.SoDiem,
                entry.DiemSau, entry.LyDo, entry.ThoiDiem))
            .ToListAsync(ct);
        var summary = await db.DocMotDongAsync("""
            SELECT COALESCE(SUM(tong_thanh_toan),0), MAX(thanh_toan_luc)
            FROM hoa_don WHERE khach_hang_id=@id AND trang_thai='DA_THANH_TOAN'
            """, r => new { Total = Convert.ToInt64(r[0]), Last = r.IsDBNull(1) ? (DateTime?)null : r.VietnamDateTime(1) }, ct, ("@id", id));
        var balance = await db.KhachHangs.AsNoTracking().Where(x => x.Id == id).Select(x => x.DiemHienCo).SingleAsync(ct);
        var customer = new CustomerRow(id, input.Name, admin ? input.Phone : CustomerRules.MaskPhone(input.Phone),
            balance, summary!.Total, summary.Last, input.Version);
        return new(customer, invoices.Select(row=>row with {PaidAt=ConvenienceStore.Web.Shared.Api.GioVietNam.ThanhOffset(row.PaidAt).DateTime}).ToList(),
            points.Select(row=>row with {At=ConvenienceStore.Web.Shared.Api.GioVietNam.ThanhOffset(row.At).DateTime}).ToList());
    }

    public async Task DeleteAsync(long id, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        await LockAsync(id, ct);
        if (Convert.ToInt32(await db.DocMotGiaTriAsync("""
            SELECT COUNT(*) FROM gio_hang WHERE khach_hang_id=@id AND trang_thai IN ('DANG_BAN','TAM_GIU')
            """, ct, ("@id", id))) > 0)
            throw LoiNghiepVu.XungDot(MaLoiChung.XungDotDuLieu, "Khách đang gắn với đơn đang bán/tạm giữ. Hãy gỡ khách trước.");
        var references = Convert.ToInt32(await db.DocMotGiaTriAsync("""
            SELECT (SELECT COUNT(*) FROM hoa_don WHERE khach_hang_id=@id)
                 + (SELECT COUNT(*) FROM lich_su_diem WHERE khach_hang_id=@id)
                 + (SELECT COUNT(*) FROM gio_hang WHERE khach_hang_id=@id)
                 + (SELECT COUNT(*) FROM phieu_doi_tra WHERE khach_hang_id=@id)
            """, ct, ("@id", id)));
        if (references == 0)
            await db.ChayAsync("DELETE FROM khach_hang WHERE id=@id", ct, ("@id", id));
        else
            await db.ChayAsync("""
                UPDATE khach_hang SET ho_ten=NULL,so_dien_thoai=NULL,email=NULL,ngay_sinh=NULL,gioi_tinh=NULL,
                trang_thai='DA_AN',xoa_du_lieu_luc=NOW(),phien_ban=phien_ban+1 WHERE id=@id
                """, ct, ("@id", id));
        await tx.CommitAsync(ct);
    }

    public async Task AdjustAsync(long id, PointAdjustment input, long actor, CancellationToken ct)
    {
        // Final scope: freeze manual adjustments, including callers bypassing MVC.
        // Automatic awards/recovery and historical ledgers use CustomerPoints independently.
        await EnsureManualAdjustmentAvailableAsync();
        if (string.IsNullOrWhiteSpace(input.Reason) || input.Reason.Trim().Length > 500)
            throw LoiNghiepVu.DuLieuKhongHopLe("reason", "Lý do bắt buộc, tối đa 500 ký tự.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        await LockAsync(id, ct);
        if (input.InvoiceId.HasValue && !await db.HoaDons.AnyAsync(x => x.Id == input.InvoiceId && x.KhachHangId == id, ct))
            throw LoiNghiepVu.DuLieuKhongHopLe("invoiceId", "Hóa đơn không thuộc khách hàng này.");
        await CustomerPoints.ChangeAsync(db, id, input.Points, "DIEU_CHINH", actor,
            input.InvoiceId, null, input.Reason.Trim(), ct);
        await tx.CommitAsync(ct);
    }

    private static Task EnsureManualAdjustmentAvailableAsync() => Task.FromException(
        new LoiNghiepVu("CHUC_NANG_DANG_KHOA", StatusCodes.Status403Forbidden,
            "KH-08 đang khóa trong phạm vi hiện tại."));

    private async Task<long> LockAsync(long id, CancellationToken ct)
    {
        var value = await db.DocMotGiaTriAsync("SELECT diem_hien_co FROM khach_hang WHERE id=@id AND trang_thai='HOAT_DONG' FOR UPDATE", ct, ("@id", id));
        return value == null ? throw LoiNghiepVu.KhongTimThay("Không tìm thấy khách hàng.") : Convert.ToInt64(value);
    }
    private static void Validate(CustomerInput input)
    {
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        if (!System.ComponentModel.DataAnnotations.Validator.TryValidateObject(input, new(input), results, true)
            || string.IsNullOrWhiteSpace(input.Name))
            throw LoiNghiepVu.DuLieuKhongHopLe("customer", "Kiểm tra họ tên, số điện thoại, email và giới tính.");
    }
}
