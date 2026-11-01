using System.Text;
using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Models.Pos;
using ConvenienceStore.Web.Shared.Data;

namespace ConvenienceStore.Web.Services.Pos;

// Đọc danh sách hóa đơn từ bảng hoa_don (schema_v1.sql).
// Mọi giá trị người dùng nhập đều đi qua tham số SQL; chỉ số trang/số dòng (số nguyên đã kiểm tra)
// được nối vào câu lệnh.
public sealed class MySqlPosInvoiceListStore : IPosInvoiceListStore
{
    private readonly CuaHangDbContext _db;

    public MySqlPosInvoiceListStore(CuaHangDbContext db)
    {
        _db = db;
    }

    public async Task<PosInvoiceListResult> SearchAsync(
        PosInvoiceListFilter filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        // [00:00 ngày đầu, 00:00 ngày sau ngày cuối)
        var from = filter.From.ToDateTime(TimeOnly.MinValue);
        var to = filter.To.AddDays(1).ToDateTime(TimeOnly.MinValue);

        var where = new StringBuilder(
            "h.thanh_toan_luc >= @tu AND h.thanh_toan_luc < @den");
        var parameters = new List<(string Ten, object? GiaTri)>
        {
            ("@tu", from),
            ("@den", to)
        };

        if (filter.CashierId is int cashierId)
        {
            where.Append(" AND h.thu_ngan_id = @thuNgan");
            parameters.Add(("@thuNgan", cashierId));
        }

        if (filter.Status is "DA_THANH_TOAN" or "DA_HUY")
        {
            where.Append(" AND h.trang_thai = @trangThai");
            parameters.Add(("@trangThai", filter.Status));
        }

        if (!string.IsNullOrWhiteSpace(filter.Code))
        {
            where.Append(" AND h.ma_hoa_don LIKE CONCAT('%', @ma, '%') ESCAPE '!'");
            parameters.Add(("@ma", EscapeLike(filter.Code.Trim())));
        }

        if (!string.IsNullOrWhiteSpace(filter.Customer))
        {
            where.Append(" AND (k.ho_ten LIKE CONCAT('%', @khach, '%') ESCAPE '!' OR k.so_dien_thoai LIKE CONCAT('%', @khach, '%') ESCAPE '!' OR (h.khach_hang_id IS NULL AND 'Khách lẻ' LIKE CONCAT('%', @khach, '%') ESCAPE '!'))");
            parameters.Add(("@khach", EscapeLike(filter.Customer.Trim())));
        }

        var args = parameters.ToArray();

        // 1) Tổng cộng trên TOÀN BỘ kết quả lọc, không chỉ trang hiện tại.
        var summarySql = $"""
            SELECT COUNT(*),
                   COALESCE(SUM(CASE WHEN h.trang_thai = 'DA_THANH_TOAN'
                                     THEN h.tong_thanh_toan END), 0),
                   COALESCE(SUM(CASE WHEN h.trang_thai = 'DA_HUY' THEN 1 ELSE 0 END), 0)
            FROM hoa_don h
            LEFT JOIN khach_hang k ON k.id = h.khach_hang_id
            WHERE {where}
            """;

        var summary = await _db.DocMotDongAsync(
            summarySql,
            r => new Summary(r.SoNguyen(0), r.SoNguyenDai(1), r.SoNguyen(2)),
            cancellationToken,
            args) ?? new Summary(0, 0, 0);

        if (summary.Count == 0)
        {
            return new PosInvoiceListResult([], 0, 0, 0, 1, 1);
        }

        var totalPages = Math.Max(
            1, (int)Math.Ceiling(summary.Count / (double)PosInvoiceListFilter.PageSize));
        var page = Math.Clamp(filter.Page, 1, totalPages);
        var offset = (page - 1) * PosInvoiceListFilter.PageSize;

        // 2) Dòng của trang hiện tại.
        var listSql = $"""
            SELECT h.id, h.ma_hoa_don, h.thanh_toan_luc, t.ho_ten,
                   h.khach_hang_id, k.ho_ten, h.tong_thanh_toan, h.trang_thai
            FROM hoa_don h
            JOIN tai_khoan t ON t.id = h.thu_ngan_id
            LEFT JOIN khach_hang k ON k.id = h.khach_hang_id
            WHERE {where}
            ORDER BY h.thanh_toan_luc DESC, h.id DESC
            LIMIT {PosInvoiceListFilter.PageSize} OFFSET {offset}
            """;

        var items = await _db.DocDanhSachAsync(
            listSql,
            r => new PosInvoiceListItem
            {
                Id = r.SoNguyenDai(0),
                Code = r.Chuoi(1),
                PaidAt = r.NgayGio(2),
                CashierName = r.Chuoi(3),
                CustomerName = r.IsDBNull(4)
                    ? "Khách lẻ"
                    : r.ChuoiHoacNull(5) ?? "Khách hàng (chưa có tên)",
                Total = r.SoNguyenDai(6),
                Status = r.Chuoi(7)
            },
            cancellationToken,
            args);

        return new PosInvoiceListResult(
            items,
            summary.Count,
            summary.Revenue,
            summary.Cancelled,
            page,
            totalPages);
    }

    // Các thu ngân đã có hóa đơn, dùng cho ô lọc của người có quyền xem tất cả.
    public async Task<IReadOnlyList<PosCashierOption>> GetCashiersAsync(
        CancellationToken cancellationToken = default) =>
        await _db.DocDanhSachAsync(
            """
            SELECT DISTINCT t.id, t.ho_ten
            FROM tai_khoan t
            JOIN hoa_don h ON h.thu_ngan_id = t.id
            ORDER BY t.ho_ten
            """,
            r => new PosCashierOption(r.SoNguyen(0), r.Chuoi(1)),
            cancellationToken);

    private sealed record Summary(int Count, long Revenue, int Cancelled);

    // Thoát ký tự đặc biệt của LIKE để "%" hoặc "_" người dùng gõ chỉ được hiểu là ký tự thường.
    private static string EscapeLike(string value) =>
        value.Replace("!", "!!")
             .Replace("%", "!%")
             .Replace("_", "!_");
}
