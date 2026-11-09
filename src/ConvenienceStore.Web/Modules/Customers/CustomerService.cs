using System.Data;
using System.Data.Common;
using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Shared.Data;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Modules.Customers;

public sealed class CustomerService(CuaHangDbContext db)
{
    public async Task<List<CustomerRow>> SearchAsync(string? search, bool admin, CancellationToken ct)
    {
        search = search?.Trim() ?? "";
        var rows = await db.DocDanhSachAsync("""
            SELECT k.id, k.ho_ten, k.so_dien_thoai, k.diem_hien_co,
              COALESCE(h.tong, 0), h.lan_cuoi, k.phien_ban
            FROM khach_hang k LEFT JOIN (
              SELECT khach_hang_id, SUM(tong_thanh_toan) tong, MAX(thanh_toan_luc) lan_cuoi
              FROM hoa_don WHERE trang_thai = 'DA_THANH_TOAN' GROUP BY khach_hang_id
            ) h ON h.khach_hang_id = k.id
            WHERE k.trang_thai = 'HOAT_DONG' AND
              (@q = '' OR k.ho_ten LIKE @like OR k.so_dien_thoai LIKE @like OR CAST(k.id AS CHAR) = @q)
            ORDER BY k.id DESC LIMIT 100
            """, r => ReadRow(r, admin), ct, ("@q", search), ("@like", "%" + search + "%"));
        return rows;
    }

    public async Task<CustomerInput> GetInputAsync(uint id, CancellationToken ct)
    {
        var k = await db.KhachHangs.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.TrangThai == "HOAT_DONG", ct)
            ?? throw LoiNghiepVu.KhongTimThay("Không tìm thấy khách hàng.");
        return new CustomerInput { Name = k.HoTen!, Phone = k.SoDienThoai!, Email = k.Email,
            BirthDate = k.NgaySinh, Gender = k.GioiTinh, Version = k.PhienBan };
    }

    public async Task<uint> SaveAsync(uint? id, CustomerInput input, int actor, CancellationToken ct)
    {
        Validate(input);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        if (id.HasValue) await LockAsync(id.Value, ct);
        var duplicate = await db.DocMotGiaTriAsync("""
            SELECT id FROM khach_hang WHERE so_dien_thoai = @phone AND (@id IS NULL OR id <> @id)
            """, ct, ("@phone", input.Phone), ("@id", id));
        if (duplicate != null)
            throw LoiNghiepVu.XungDot(MaLoiChung.TrungDuLieu,
                "Số điện thoại đã tồn tại. Hãy chọn khách đã có.", new { customerId = Convert.ToUInt32(duplicate) });
        try
        {
            if (id.HasValue)
            {
                var changed = await db.ChayAsync("""
                    UPDATE khach_hang SET ho_ten=@name, so_dien_thoai=@phone, email=@email,
                    ngay_sinh=@birth, gioi_tinh=@gender, phien_ban=phien_ban+1
                    WHERE id=@id AND phien_ban=@version AND trang_thai='HOAT_DONG'
                    """, ct, Parameters(input, id, actor));
                if (changed != 1) throw LoiNghiepVu.XungDot(MaLoiChung.XungDotDuLieu, "Khách đã thay đổi. Hãy tải lại trước khi sửa.");
            }
            else
            {
                await db.ChayAsync("""
                    INSERT INTO khach_hang (ho_ten,so_dien_thoai,email,ngay_sinh,gioi_tinh,nguoi_tao_id)
                    VALUES (@name,@phone,@email,@birth,@gender,@actor)
                    """, ct, Parameters(input, null, actor));
                id = Convert.ToUInt32(await db.DocMotGiaTriAsync("SELECT LAST_INSERT_ID()", ct));
            }
        }
        catch (DbException ex) when (TruyVanCsdl.LaLoiTrungKhoa(ex, "uq_khach_hang_sdt"))
        {
            throw LoiNghiepVu.XungDot(MaLoiChung.TrungDuLieu, "Số điện thoại đã tồn tại. Hãy tìm lại khách.");
        }
        await tx.CommitAsync(ct);
        return id!.Value;
    }

    public async Task<CustomerHistory> HistoryAsync(uint id, bool admin, CancellationToken ct)
    {
        var input = await GetInputAsync(id, ct);
        var invoices = await db.DocDanhSachAsync("""
            SELECT id,ma_hoa_don,thanh_toan_luc,tong_thanh_toan,diem_cong
            FROM hoa_don WHERE khach_hang_id=@id ORDER BY thanh_toan_luc DESC,id DESC
            """, r => new CustomerInvoice(Convert.ToUInt32(r[0]), r.GetString(1), r.GetDateTime(2),
                Convert.ToUInt64(r[3]), Convert.ToUInt32(r[4])), ct, ("@id", id));
        var points = await db.DocDanhSachAsync("""
            SELECT id,loai,so_diem,diem_sau,ly_do,thoi_diem FROM lich_su_diem
            WHERE khach_hang_id=@id ORDER BY thoi_diem DESC,id DESC
            """, r => new CustomerPoint(Convert.ToUInt64(r[0]), r.GetString(1), Convert.ToInt32(r[2]),
                Convert.ToUInt32(r[3]), r.IsDBNull(4) ? null : r.GetString(4), r.GetDateTime(5)), ct, ("@id", id));
        var summary = await db.DocMotDongAsync("""
            SELECT COALESCE(SUM(tong_thanh_toan),0), MAX(thanh_toan_luc)
            FROM hoa_don WHERE khach_hang_id=@id AND trang_thai='DA_THANH_TOAN'
            """, r => new { Total = Convert.ToUInt64(r[0]), Last = r.IsDBNull(1) ? (DateTime?)null : r.GetDateTime(1) }, ct, ("@id", id));
        var balance = await db.KhachHangs.AsNoTracking().Where(x => x.Id == id).Select(x => x.DiemHienCo).SingleAsync(ct);
        var customer = new CustomerRow(id, input.Name, admin ? input.Phone : CustomerRules.MaskPhone(input.Phone),
            balance, summary!.Total, summary.Last, input.Version);
        return new(customer, invoices, points);
    }

    public async Task DeleteAsync(uint id, CancellationToken ct)
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

    public async Task AdjustAsync(uint id, PointAdjustment input, int actor, CancellationToken ct)
    {
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

    private async Task<uint> LockAsync(uint id, CancellationToken ct)
    {
        var value = await db.DocMotGiaTriAsync("SELECT diem_hien_co FROM khach_hang WHERE id=@id AND trang_thai='HOAT_DONG' FOR UPDATE", ct, ("@id", id));
        return value == null ? throw LoiNghiepVu.KhongTimThay("Không tìm thấy khách hàng.") : Convert.ToUInt32(value);
    }
    private static CustomerRow ReadRow(DbDataReader r, bool admin) => new(Convert.ToUInt32(r[0]), r.GetString(1),
        admin ? r.GetString(2) : CustomerRules.MaskPhone(r.GetString(2)), Convert.ToUInt32(r[3]),
        Convert.ToUInt64(r[4]), r.IsDBNull(5) ? null : r.GetDateTime(5), Convert.ToUInt32(r[6]));
    private static (string, object?)[] Parameters(CustomerInput i, uint? id, int actor) =>
    [("@name", i.Name.Trim()), ("@phone", i.Phone), ("@email", string.IsNullOrWhiteSpace(i.Email) ? null : i.Email.Trim()),
     ("@birth", i.BirthDate?.Date), ("@gender", string.IsNullOrEmpty(i.Gender) ? null : i.Gender),
     ("@id", id), ("@version", i.Version), ("@actor", actor)];
    private static void Validate(CustomerInput input)
    {
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        if (!System.ComponentModel.DataAnnotations.Validator.TryValidateObject(input, new(input), results, true)
            || string.IsNullOrWhiteSpace(input.Name))
            throw LoiNghiepVu.DuLieuKhongHopLe("customer", "Kiểm tra họ tên, số điện thoại, email và giới tính.");
    }
}
