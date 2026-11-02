using System.Data;
using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Returns.Models;
using ConvenienceStore.Web.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Modules.Returns.Services;

public sealed class MySqlInvoiceLookupStore(CuaHangDbContext db) : IInvoiceLookupStore
{
    public async Task<InvoiceLookupSnapshot> ReadAsync(InvoiceLookupFilter filter, DateTime nowVietnam,
        CancellationToken cancellationToken = default)
    {
        // Cùng một snapshot cho chính sách, tổng số hóa đơn và các dòng đã trả/đang giữ chỗ.
        await using var transaction = db.Database.CurrentTransaction is null
            ? await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken) : null;
        var policy = await db.DocMotDongAsync("""
            SELECT id, so_ngay_doi_tra FROM chinh_sach_doi_tra
            WHERE hieu_luc_tu <= @now ORDER BY hieu_luc_tu DESC, id DESC LIMIT 1
            """, r => new ReturnLookupPolicy(r.SoNguyen(0), r.SoNguyen(1)), cancellationToken, ("@now", nowVietnam));
        if (policy is null)
            return new InvoiceLookupSnapshot(null, [], 0, 1, filter.Size);

        // BR-DT-08: tuyệt đối không thêm điều kiện thu_ngan_id của người đang tra cứu.
        var where = filter.Ma is not null ? "h.ma_hoa_don = @ma"
            : "k.so_dien_thoai = @phone AND h.thanh_toan_luc >= @from AND h.thanh_toan_luc <= @now";
        var parameters = new (string Ten, object? GiaTri)[] {
            ("@ma", filter.Ma), ("@phone", filter.SoDienThoai), ("@now", nowVietnam),
            ("@from", DateOnly.FromDateTime(nowVietnam).AddDays(-policy.SoNgayDoiTra).ToDateTime(TimeOnly.MinValue))
        };
        var count = Convert.ToInt32(await db.DocMotGiaTriAsync($"""
            SELECT COUNT(*) FROM hoa_don h LEFT JOIN khach_hang k ON k.id = h.khach_hang_id WHERE {where}
            """, cancellationToken, parameters));
        var totalPages = Math.Max(1, (int)Math.Ceiling(count / (double)filter.Size));
        var page = Math.Clamp(filter.Page, 1, totalPages);
        var invoices = await db.DocDanhSachAsync($"""
            SELECT h.id, h.ma_hoa_don, h.thanh_toan_luc, h.thu_ngan_id, t.ho_ten,
                   h.khach_hang_id, k.ho_ten, k.so_dien_thoai, h.tong_thanh_toan, h.trang_thai
            FROM hoa_don h
            JOIN tai_khoan t ON t.id = h.thu_ngan_id
            LEFT JOIN khach_hang k ON k.id = h.khach_hang_id
            WHERE {where} ORDER BY h.thanh_toan_luc DESC, h.id DESC
            LIMIT @size OFFSET @offset
            """, r => new OriginalInvoiceFact {
                Id = r.SoNguyenDai(0), Ma = r.Chuoi(1), NgayMua = r.NgayGio(2), ThuNganId = r.SoNguyen(3),
                TenThuNgan = r.Chuoi(4), KhachHangId = r.IsDBNull(5) ? null : r.SoNguyen(5),
                TenKhachHang = r.ChuoiHoacNull(6), SoDienThoai = r.ChuoiHoacNull(7),
                TongThanhToan = r.SoNguyenDai(8), TrangThai = r.Chuoi(9)
            }, cancellationToken, [.. parameters, ("@size", filter.Size), ("@offset", (page - 1) * filter.Size)]);
        if (invoices.Count > 0)
        {
            var (ids, args) = TruyVanCsdl.TaoThamSoIn("hd", invoices.Select(i => checked((int)i.Id)));
            var lines = await db.DocDanhSachAsync($"""
                SELECT d.hoa_don_id, d.san_pham_id, d.ten_san_pham, d.don_vi_tinh,
                       d.so_luong, d.don_gia, d.giam_don_vi, dm.ten, dm.khong_doi_tra,
                       COALESCE(r.da_tra, 0)
                FROM ct_hoa_don d
                JOIN san_pham sp ON sp.id = d.san_pham_id
                JOIN danh_muc dm ON dm.id = sp.danh_muc_id
                LEFT JOIN (
                    SELECT c.hoa_don_id, c.san_pham_id, SUM(c.so_luong) AS da_tra
                    FROM ct_phieu_doi_tra c JOIN phieu_doi_tra p ON p.id = c.phieu_doi_tra_id
                    WHERE c.hoa_don_id IN ({ids}) AND p.trang_thai IN ('CHO_DUYET', 'DA_DUYET', 'HOAN_TAT')
                    GROUP BY c.hoa_don_id, c.san_pham_id
                ) r ON r.hoa_don_id = d.hoa_don_id AND r.san_pham_id = d.san_pham_id
                WHERE d.hoa_don_id IN ({ids}) ORDER BY d.hoa_don_id, d.san_pham_id
                """, r => (InvoiceId: r.SoNguyenDai(0), Line: new OriginalInvoiceLineFact(
                    r.SoNguyen(1), r.Chuoi(2), r.Chuoi(3), r.SoNguyen(4), r.SoNguyenDai(5),
                    r.SoNguyenDai(6), r.Chuoi(7), r.DungSai(8), r.SoNguyenDai(9))), cancellationToken, args);
            var byId = invoices.ToDictionary(i => i.Id);
            foreach (var line in lines) byId[line.InvoiceId].ChiTiet.Add(line.Line);
        }
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return new InvoiceLookupSnapshot(policy, invoices, count, page, filter.Size);
    }
}
