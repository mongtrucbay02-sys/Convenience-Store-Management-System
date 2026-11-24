using System.Data.Common;
using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Returns.Models;
using ConvenienceStore.Web.Shared.Api;
using ConvenienceStore.Web.Shared.Data;

namespace ConvenienceStore.Web.Modules.Returns.Services;

public sealed class ReturnSlipLookupService(CuaHangDbContext db)
{
    private const int PageSize = 50;
    private const string Columns = """
        SELECT p.id, p.ma_phieu, p.loai, h.ma_hoa_don, p.ngay_lap, t.ho_ten,
               p.tien_hoan, p.tien_thu_them, p.trang_thai
        """;
    private const string Tables = """
        FROM phieu_doi_tra p
        JOIN hoa_don h ON h.id = p.hoa_don_goc_id
        JOIN tai_khoan t ON t.id = p.thu_ngan_id
        """;

    public async Task<ReturnSlipList> ListAsync(ReturnSlipQuery query, int? cashier, CancellationToken ct)
    {
        var conditions = new List<string>();
        var args = new List<(string, object?)>();
        RestrictToCashier(cashier, conditions, args);

        if (!string.IsNullOrWhiteSpace(query.Code))
        {
            conditions.Add("p.ma_phieu = @code");
            args.Add(("@code", query.Code.Trim().ToUpperInvariant()));
        }
        if (!string.IsNullOrWhiteSpace(query.InvoiceCode))
        {
            conditions.Add("h.ma_hoa_don = @invoiceCode");
            args.Add(("@invoiceCode", query.InvoiceCode.Trim().ToUpperInvariant()));
        }
        if (query.From is DateOnly from)
        {
            conditions.Add("p.ngay_lap >= @from");
            args.Add(("@from", from.ToDateTime(TimeOnly.MinValue)));
        }
        if (query.To is DateOnly to && to < DateOnly.MaxValue)
        {
            conditions.Add("p.ngay_lap < @to");
            args.Add(("@to", to.AddDays(1).ToDateTime(TimeOnly.MinValue)));
        }
        if (!string.IsNullOrEmpty(query.Status))
        {
            conditions.Add("p.trang_thai = @status");
            args.Add(("@status", query.Status));
        }
        if (!string.IsNullOrEmpty(query.Reason))
        {
            conditions.Add("EXISTS (SELECT 1 FROM ct_phieu_doi_tra c WHERE c.phieu_doi_tra_id = p.id AND c.ly_do = @reason)");
            args.Add(("@reason", query.Reason));
        }

        var where = conditions.Count == 0 ? "" : "WHERE " + string.Join(" AND ", conditions);
        args.Add(("@limit", PageSize + 1));
        args.Add(("@offset", (query.Page - 1) * PageSize));
        var rows = await db.DocDanhSachAsync(
            $"{Columns} {Tables} {where} ORDER BY p.ngay_lap DESC, p.id DESC LIMIT @limit OFFSET @offset",
            ReadRow, ct, args.ToArray());

        return new(query, rows.Take(PageSize).ToArray(), rows.Count > PageSize);
    }

    public async Task<ReturnSlipDetails?> FindAsync(long id, int? cashier, CancellationToken ct)
    {
        var conditions = new List<string> { "p.id = @id" };
        var args = new List<(string, object?)> { ("@id", id) };
        RestrictToCashier(cashier, conditions, args);
        var detail = await db.DocMotDongAsync($"""
            {Columns}, hm.ma_hoa_don, p.gia_tri_hang_tra, p.diem_thu_hoi,
                ad.ho_ten, p.duyet_luc, p.ghi_chu_duyet, p.hoan_tat_luc
            {Tables}
            LEFT JOIN hoa_don hm ON hm.id = p.hoa_don_moi_id
            LEFT JOIN tai_khoan ad ON ad.id = p.nguoi_duyet_id
            WHERE {string.Join(" AND ", conditions)}
            """, r => new ReturnSlipDetails(ReadRow(r), r.ChuoiHoacNull(9), r.SoNguyenDai(10),
            r.SoNguyen(11), r.ChuoiHoacNull(12), GioVietNam.ThanhOffset(r.NgayGioHoacNull(13)),
            r.ChuoiHoacNull(14), GioVietNam.ThanhOffset(r.NgayGioHoacNull(15)), []), ct, args.ToArray());

        if (detail is null)
            return null;

        var lines = await db.DocDanhSachAsync("""
            SELECT d.ten_san_pham, c.so_luong, c.don_gia_thuc_tra, c.ly_do, c.ghi_chu_ly_do,
                   c.tinh_trang, c.cach_xu_ly
            FROM ct_phieu_doi_tra c
            JOIN ct_hoa_don d ON d.hoa_don_id = c.hoa_don_id AND d.san_pham_id = c.san_pham_id
            WHERE c.phieu_doi_tra_id = @id
            ORDER BY c.san_pham_id
            """, r => new ReturnSlipLine(r.Chuoi(0), r.SoNguyen(1), r.SoNguyenDai(2), r.Chuoi(3),
            r.ChuoiHoacNull(4), r.ChuoiHoacNull(5), r.Chuoi(6)), ct, ("@id", id));
        return detail with { Lines = lines };
    }

    private static void RestrictToCashier(int? cashier, List<string> conditions, List<(string, object?)> args)
    {
        if (cashier.HasValue)
        {
            conditions.Add("p.thu_ngan_id = @cashier");
            args.Add(("@cashier", cashier.Value));
        }
    }

    private static ReturnSlipRow ReadRow(DbDataReader r) => new(r.SoNguyenDai(0), r.Chuoi(1), r.Chuoi(2),
        r.Chuoi(3), GioVietNam.ThanhOffset(r.NgayGio(4)), r.Chuoi(5), r.SoNguyenDai(6),
        r.SoNguyenDai(7), r.Chuoi(8));
}
