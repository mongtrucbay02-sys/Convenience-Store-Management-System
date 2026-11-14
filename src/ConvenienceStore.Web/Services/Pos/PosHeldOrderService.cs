
using System.Data;
using System.Security.Claims;
using ConvenienceStore.Web.Modules.Auth;
using ConvenienceStore.Web.Modules.Auth.Security;
using MySqlConnector;

namespace ConvenienceStore.Web.Services.Pos;

public sealed record PosHeldOrder(
    int Id,
    DateTime HeldAt,
    string Note,
    int Quantity,
    long Total);

public sealed record PosHeldOrderList(
    int ExpiredCount,
    IReadOnlyList<PosHeldOrder> Orders);

public sealed record PosHeldOrderResult(
    bool Success,
    string Message);

/// <summary>
/// POS-08: Quản lý đơn hàng tạm giữ.
/// Admin (AD) và Thu ngân (TN) có phạm vi đơn hàng riêng.
/// Không giữ chỗ hoặc thay đổi tồn kho khi tạm giữ đơn.
/// </summary>
public sealed class PosHeldOrderService(
    MySqlPosCatalog catalog,
    IHttpContextAccessor http)
{
    // ID tài khoản đăng nhập hiện tại.
    // Admin và Thu ngân đều sử dụng ID tài khoản của chính mình.
    private int AccountId
    {
        get
        {
            var user = http.HttpContext?.User;

            bool isAdmin = user?.IsInRole("AD") == true;
            bool isCashier =
                user?.IsInRole(BangQuyen.ThuNgan) == true;

            if (user?.Identity?.IsAuthenticated != true
                || (!isAdmin && !isCashier)
                || user.DangPhaiDoiMatKhau()
                || !int.TryParse(
                    user.FindFirstValue(ClaimTypes.NameIdentifier),
                    out int id)
                || id <= 0)
            {
                throw new UnauthorizedAccessException(
                    "Cần tài khoản Admin hoặc Thu ngân hợp lệ.");
            }

            return id;
        }
    }

    // Thực thi câu lệnh SQL có tham số.
    private static async Task<object?> Execute(
        MySqlConnection conn,
        MySqlTransaction tx,
        string sql,
        params (string Name, object? Value)[] parameters)
    {
        await using var cmd = new MySqlCommand(sql, conn, tx);

        foreach (var (name, value) in parameters)
        {
            cmd.Parameters.AddWithValue(
                name,
                value ?? DBNull.Value);
        }

        return await cmd.ExecuteScalarAsync();
    }

    // Mở kết nối và transaction, kiểm tra tài khoản AD/TN.
    private async Task<(
        MySqlConnection Connection,
        MySqlTransaction Transaction,
        int AccountId)> Begin()
    {
        var accountId = AccountId;
        var conn = catalog.CreateConnection();

        try
        {
            await conn.OpenAsync();

            await using (var zone = new MySqlCommand(
                "SET time_zone = '+07:00'", conn))
            {
                await zone.ExecuteNonQueryAsync();
            }

            var tx = await conn.BeginTransactionAsync(
                IsolationLevel.ReadCommitted);

            try
            {
                // Khóa dòng tài khoản để tuần tự hóa
                // thao tác giữ và gọi lại đơn của cùng tài khoản.
                var found = await Execute(conn, tx, """
                    SELECT t.id
                    FROM tai_khoan t
                    INNER JOIN vai_tro v
                        ON v.id = t.vai_tro_id
                    WHERE t.id = @t
                      AND v.ma IN ('AD', 'TN')
                      AND t.trang_thai = 'HOAT_DONG'
                      AND t.bat_buoc_doi_mk = FALSE
                    FOR UPDATE
                    """,
                    ("@t", accountId));

                if (found is null || found is DBNull)
                {
                    throw new UnauthorizedAccessException(
                        "Tài khoản Admin hoặc Thu ngân không hợp lệ.");
                }

                return (conn, tx, accountId);
            }
            catch
            {
                await tx.DisposeAsync();
                throw;
            }
        }
        catch
        {
            await conn.DisposeAsync();
            throw;
        }
    }

    // Tự động hủy các đơn tạm giữ đã hết hạn.
    // Chỉ xử lý đơn thuộc tài khoản đang đăng nhập.
    private static async Task<int> Expire(
        MySqlConnection conn,
        MySqlTransaction tx,
        int accountId)
    {
        await using var cmd = new MySqlCommand("""
            UPDATE gio_hang g
            SET
                g.trang_thai = 'DA_HUY',
                g.huy_luc = NOW(),
                g.ly_do_huy = 'HET_HAN_TAM_GIU',
                g.gia_tri_khi_huy = COALESCE(
                    (
                        SELECT SUM(
                            ct.so_luong *
                            (ct.don_gia - ct.giam_don_vi)
                        )
                        FROM ct_gio_hang ct
                        WHERE ct.gio_hang_id = g.id
                    ),
                    0
                ),
                g.phien_ban = g.phien_ban + 1
            WHERE g.thu_ngan_id = @t
              AND g.trang_thai = 'TAM_GIU'
              AND g.tam_giu_luc < CURDATE()
            """, conn, tx);

        cmd.Parameters.AddWithValue("@t", accountId);

        return await cmd.ExecuteNonQueryAsync();
    }

    // =====================================================
    // POS-08: DANH SÁCH ĐƠN TẠM GIỮ
    // =====================================================

    public async Task<PosHeldOrderList> ListAsync()
    {
        var scope = await Begin();

        await using var conn = scope.Connection;
        await using var tx = scope.Transaction;

        int expired = await Expire(
            conn,
            tx,
            scope.AccountId);

        var orders = new List<PosHeldOrder>();

        await using (var cmd = new MySqlCommand("""
            SELECT
                g.id,
                g.tam_giu_luc,
                COALESCE(g.ghi_chu_tam_giu, ''),
                COALESCE(SUM(ct.so_luong), 0),
                COALESCE(
                    SUM(
                        ct.so_luong *
                        (ct.don_gia - ct.giam_don_vi)
                    ),
                    0
                )
            FROM gio_hang g
            LEFT JOIN ct_gio_hang ct
                ON ct.gio_hang_id = g.id
            WHERE g.thu_ngan_id = @t
              AND g.trang_thai = 'TAM_GIU'
            GROUP BY
                g.id,
                g.tam_giu_luc,
                g.ghi_chu_tam_giu
            ORDER BY g.tam_giu_luc, g.id
            """, conn, tx))
        {
            cmd.Parameters.AddWithValue(
                "@t",
                scope.AccountId);

            await using var reader =
                await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                orders.Add(new PosHeldOrder(
                    Convert.ToInt32(reader[0]),
                    reader.GetDateTime(1),
                    reader.GetString(2),
                    Convert.ToInt32(reader[3]),
                    Convert.ToInt64(reader[4])));
            }
        }

        await tx.CommitAsync();

        return new PosHeldOrderList(expired, orders);
    }

    // =====================================================
    // POS-08: TẠM GIỮ ĐƠN HIỆN TẠI
    // =====================================================

    public async Task<PosHeldOrderResult> HoldAsync(
        string? note)
    {
        note = note?.Trim();

        if (note?.Length > 100)
        {
            return new(
                false,
                "Ghi chú tối đa 100 ký tự.");
        }

        var scope = await Begin();

        await using var conn = scope.Connection;
        await using var tx = scope.Transaction;

        await Expire(conn, tx, scope.AccountId);

        int count = Convert.ToInt32(
            await Execute(conn, tx, """
                SELECT COUNT(*)
                FROM gio_hang
                WHERE thu_ngan_id = @t
                  AND trang_thai = 'TAM_GIU'
                """,
                ("@t", scope.AccountId)));

        if (count >= 5)
        {
            return new(
                false,
                "Bạn đã có 5 đơn tạm giữ. Hãy gọi lại một đơn trước khi tạm giữ thêm.");
        }

        var current = await Execute(conn, tx, """
            SELECT id
            FROM gio_hang
            WHERE thu_ngan_id = @t
              AND trang_thai = 'DANG_BAN'
            FOR UPDATE
            """,
            ("@t", scope.AccountId));

        if (current is null || current is DBNull)
        {
            return new(
                false,
                "Giỏ hàng đang trống, không có đơn để tạm giữ.");
        }

        int id = Convert.ToInt32(current);

        int lines = Convert.ToInt32(
            await Execute(conn, tx, """
                SELECT COUNT(*)
                FROM ct_gio_hang
                WHERE gio_hang_id = @g
                """,
                ("@g", id)));

        if (lines == 0)
        {
            return new(
                false,
                "Giỏ hàng đang trống, không có đơn để tạm giữ.");
        }

        await Execute(conn, tx, """
            UPDATE gio_hang
            SET
                trang_thai = 'TAM_GIU',
                tam_giu_luc = NOW(),
                ghi_chu_tam_giu = @n,
                phien_ban = phien_ban + 1
            WHERE id = @g
              AND trang_thai = 'DANG_BAN'
            """,
            ("@n", note),
            ("@g", id));

        // Tạo giỏ hàng DANG_BAN mới cho chính tài khoản này.
        await Execute(conn, tx, """
            INSERT INTO gio_hang (thu_ngan_id)
            VALUES (@t)
            """,
            ("@t", scope.AccountId));

        await tx.CommitAsync();

        return new(
            true,
            $"Đã tạm giữ đơn #{id} và tạo giỏ trống mới. Đơn không giữ chỗ tồn kho.");
    }

    // Dữ liệu chi tiết sản phẩm trong đơn tạm giữ.
    private sealed record Line(
        int Id,
        string Name,
        int Quantity,
        long Price,
        int? Promotion,
        long Discount,
        long? CurrentPrice,
        int Stock,
        bool Active);

    // =====================================================
    // POS-08: GỌI LẠI ĐƠN TẠM GIỮ
    // =====================================================

    public async Task<PosHeldOrderResult> RecallAsync(
        int orderId)
    {
        if (orderId <= 0)
        {
            return new(
                false,
                "Mã đơn tạm giữ không hợp lệ.");
        }

        var scope = await Begin();

        await using var conn = scope.Connection;
        await using var tx = scope.Transaction;

        await Expire(conn, tx, scope.AccountId);

        var current = await Execute(conn, tx, """
            SELECT id
            FROM gio_hang
            WHERE thu_ngan_id = @t
              AND trang_thai = 'DANG_BAN'
            FOR UPDATE
            """,
            ("@t", scope.AccountId));

        // Không gọi lại nếu giỏ hiện tại đang có sản phẩm.
        if (current is not null && current is not DBNull)
        {
            int currentId = Convert.ToInt32(current);

            int currentLines = Convert.ToInt32(
                await Execute(conn, tx, """
                    SELECT COUNT(*)
                    FROM ct_gio_hang
                    WHERE gio_hang_id = @g
                    """,
                    ("@g", currentId)));

            if (currentLines > 0)
            {
                return new(
                    false,
                    "Giỏ hiện tại còn sản phẩm. Hãy thanh toán, tạm giữ hoặc hủy giỏ hiện tại trước khi gọi lại.");
            }
        }

        // Chỉ được gọi lại đơn của chính tài khoản đang đăng nhập.
        var target = await Execute(conn, tx, """
            SELECT id
            FROM gio_hang
            WHERE id = @g
              AND thu_ngan_id = @t
              AND trang_thai = 'TAM_GIU'
              AND tam_giu_luc >= CURDATE()
            FOR UPDATE
            """,
            ("@g", orderId),
            ("@t", scope.AccountId));

        if (target is null || target is DBNull)
        {
            return new(
                false,
                "Không tìm thấy đơn tạm giữ còn hiệu lực của bạn.");
        }

        var lines = new List<Line>();

        await using (var cmd = new MySqlCommand("""
            SELECT
                ct.san_pham_id,
                sp.ten_san_pham,
                ct.so_luong,
                ct.don_gia,
                ct.khuyen_mai_id,
                ct.giam_don_vi,
                gia.gia_ban,
                COALESCE(tk.so_luong_ton, 0),
                (
                    sp.trang_thai = 'DANG_KINH_DOANH'
                    AND dm.trang_thai = 'HOAT_DONG'
                )
            FROM ct_gio_hang ct
            JOIN san_pham sp
                ON sp.id = ct.san_pham_id
            JOIN danh_muc dm
                ON dm.id = sp.danh_muc_id
            LEFT JOIN v_gia_ban_hien_hanh gia
                ON gia.san_pham_id = sp.id
            LEFT JOIN ton_kho tk
                ON tk.san_pham_id = sp.id
            WHERE ct.gio_hang_id = @g
            ORDER BY ct.san_pham_id
            FOR SHARE
            """, conn, tx))
        {
            cmd.Parameters.AddWithValue("@g", orderId);

            await using var reader =
                await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                lines.Add(new Line(
                    Convert.ToInt32(reader[0]),
                    reader.GetString(1),
                    Convert.ToInt32(reader[2]),
                    Convert.ToInt64(reader[3]),
                    reader.IsDBNull(4)
                        ? null
                        : Convert.ToInt32(reader[4]),
                    Convert.ToInt64(reader[5]),
                    reader.IsDBNull(6)
                        ? null
                        : Convert.ToInt64(reader[6]),
                    Convert.ToInt32(reader[7]),
                    Convert.ToBoolean(reader[8])));
            }
        }

        if (lines.Count == 0)
        {
            return new(
                false,
                "Đơn tạm giữ không có sản phẩm.");
        }

        // Kiểm tra trạng thái sản phẩm, giá và tồn kho.
        var unavailable = lines
            .Where(l =>
                !l.Active
                || l.CurrentPrice is null or <= 0
                || l.CurrentPrice % 100 != 0
                || l.Stock < l.Quantity)
            .ToList();

        if (unavailable.Count > 0)
        {
            return new(
                false,
                "Chưa gọi lại đơn: " +
                string.Join("; ", unavailable.Select(l =>
                    !l.Active
                    || l.CurrentPrice is null or <= 0
                        ? $"{l.Name} không còn kinh doanh hoặc chưa có giá hợp lệ"
                        : $"{l.Name}: cần {l.Quantity}, tồn hiện tại {l.Stock}"))
                + ". Đơn vẫn được tạm giữ, chưa thay đổi tồn kho.");
        }

        // Kiểm tra khách hàng còn hoạt động hay không.
        bool member = Convert.ToInt32(
            await Execute(conn, tx, """
                SELECT COUNT(*)
                FROM gio_hang g
                JOIN khach_hang k
                    ON k.id = g.khach_hang_id
                WHERE g.id = @g
                  AND k.trang_thai = 'HOAT_DONG'
                """,
                ("@g", orderId))) > 0;

        var promotions =
            await PosPromotionCalculator.ReadAsync(conn, tx);

        var changes = new List<string>();

        // Tính lại giá và khuyến mãi tại thời điểm gọi lại.
        foreach (var line in lines)
        {
            long price = line.CurrentPrice!.Value;

            var best = PosPromotionCalculator.Best(
                promotions,
                line.Id,
                price,
                member);

            long discount = best is null
                ? 0
                : PosPromotionCalculator.Discount(price, best);

            if (line.Price != price)
            {
                changes.Add(
                    $"{line.Name}: giá {line.Price:N0} → {price:N0} đ");
            }

            if (line.Promotion != best?.Id
                || line.Discount != discount)
            {
                changes.Add(
                    $"{line.Name}: giảm mỗi đơn vị {line.Discount:N0} → {discount:N0} đ");
            }

            await Execute(conn, tx, """
                UPDATE ct_gio_hang
                SET
                    don_gia = @gia,
                    khuyen_mai_id = @km,
                    giam_don_vi = @giam
                WHERE gio_hang_id = @g
                  AND san_pham_id = @p
                """,
                ("@gia", price),
                ("@km", best?.Id),
                ("@giam", discount),
                ("@g", orderId),
                ("@p", line.Id));
        }

        // Chỉ xóa giỏ trống hiện tại của chính tài khoản.
        // Không xóa giỏ đang có sản phẩm.
        if (current is not null && current is not DBNull)
        {
            await Execute(conn, tx, """
                DELETE FROM gio_hang
                WHERE id = @g
                  AND thu_ngan_id = @t
                  AND trang_thai = 'DANG_BAN'
                """,
                ("@g", current),
                ("@t", scope.AccountId));
        }

        // Chuyển đơn tạm giữ thành đơn đang bán.
        await Execute(conn, tx, """
            UPDATE gio_hang
            SET
                trang_thai = 'DANG_BAN',
                tam_giu_luc = NULL,
                ghi_chu_tam_giu = NULL,
                phien_ban = phien_ban + 1
            WHERE id = @g
              AND thu_ngan_id = @t
              AND trang_thai = 'TAM_GIU'
            """,
            ("@g", orderId),
            ("@t", scope.AccountId));

        await tx.CommitAsync();

        return new(
            true,
            $"Đã gọi lại đơn #{orderId}. Đã kiểm tra lại giá, khuyến mãi và tồn kho. "
            + "Tồn hiện tại: "
            + string.Join(
                "; ",
                lines.Take(8).Select(l =>
                    $"{l.Name}: {l.Stock} (cần {l.Quantity})"))
            + ". "
            + (changes.Count > 0
                ? string.Join("; ", changes.Take(8))
                    + ". Vui lòng kiểm tra tổng tiền trước khi thanh toán."
                : "Giá và khuyến mãi không đổi."));
    }
}
