using System.Data;
using ConvenienceStore.Web.Models.Pos;
using ConvenienceStore.Web.Modules.Auth;
using ConvenienceStore.Web.Modules.Auth.Security;
using MySqlConnector;

namespace ConvenienceStore.Web.Services.Pos;

/// <summary>
/// Giỏ hàng POS lưu trong CSDL (bảng gio_hang, ct_gio_hang), gắn với TÀI KHOẢN thu ngân đang đăng nhập.
/// BR-POS-03: mỗi thu ngân chỉ có một đơn DANG_BAN. Ràng buộc cuối cùng do khóa duy nhất
/// uq_gio_dang_ban của CSDL đảm bảo; code chỉ "lấy đơn đang bán, chưa có thì tạo".
/// Mã thu ngân luôn lấy từ claim của phiên đăng nhập, KHÔNG nhận từ request.
///
/// Mọi giao dịch chạy ở mức READ COMMITTED:
///  - SELECT ... FOR UPDATE trên dòng chưa tồn tại không khóa "khoảng trống" (gap lock),
///    nên hai request đầu tiên của cùng một thu ngân không bị deadlock khi cùng INSERT.
///  - Mỗi lần đọc thấy dữ liệu đã commit mới nhất, không bị "ảnh chụp cũ" của REPEATABLE READ.
/// </summary>
public sealed class PosCartService
{
    private const IsolationLevel MucCoLap = IsolationLevel.ReadCommitted;

    private readonly IHttpContextAccessor _http;
    private readonly MySqlPosCatalog _catalog;

    public PosCartService(IHttpContextAccessor http, MySqlPosCatalog catalog)
    {
        _http = http;
        _catalog = catalog;
    }

    private int ThuNganId =>
        (_http.HttpContext?.User ?? throw new InvalidOperationException("Không có ngữ cảnh HTTP."))
        .LayTaiKhoanId();

    // ---------- Đọc giỏ (đồng thời tạo đơn trống nếu chưa có: luồng chính bước 1) ----------

    public async Task<PosCart> GetCartAsync()
    {
        await using var conn = _catalog.CreateConnection();
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync(MucCoLap);

        var gioId = await LayHoacTaoGioAsync(conn, tx, ThuNganId);
        var cart = await DocDongAsync(conn, tx, gioId);

        await tx.CommitAsync();
        return cart;
    }

    // ---------- Thêm sản phẩm (qty 1, hoặc +1 nếu đã có) ----------

    public async Task<string?> AddAsync(int productId)
    {
        var product = await _catalog.FindAsync(productId);
        if (product == null)
            return "Không tìm thấy sản phẩm.";

        // BR-POS-02: tiền là số nguyên VND, bội số 100.
        if (product.Price <= 0 || product.Price % 100 != 0)
            return "Giá bán sản phẩm không hợp lệ.";

        await using var conn = _catalog.CreateConnection();
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync(MucCoLap);

        // Khóa dòng gio_hang của thu ngân: các request cùng thu ngân xếp hàng, không ghi đè nhau.
        var gioId = await LayHoacTaoGioAsync(conn, tx, ThuNganId);
        var hienCo = await DocSoLuongAsync(conn, tx, gioId, productId);
        var moi = (hienCo ?? 0) + 1;

        // E2: vượt tồn thì không cho thêm.
        if (moi > product.Stock)
        {
            await tx.RollbackAsync();
            return $"Chỉ còn {product.Stock} sản phẩm.";
        }

        if (hienCo == null)
        {
            // BR-POS-01: giá dòng chốt theo giá đang hiệu lực LÚC THÊM VÀO GIỎ.
            await using var them = new MySqlCommand(
                """
                INSERT INTO ct_gio_hang (gio_hang_id, san_pham_id, so_luong, don_gia)
                VALUES (@g, @p, 1, @gia)
                """, conn, tx);
            them.Parameters.AddWithValue("@g", gioId);
            them.Parameters.AddWithValue("@p", productId);
            them.Parameters.AddWithValue("@gia", product.Price);
            await them.ExecuteNonQueryAsync();
        }
        else
        {
            // Giữ nguyên don_gia đã chốt, chỉ tăng số lượng.
            await CapNhatSoLuongAsync(conn, tx, gioId, productId, moi);
        }

        await tx.CommitAsync();
        return null;
    }

    // ---------- Sửa số lượng / xóa dòng ----------

    public async Task<string?> UpdateAsync(int productId, int quantity)
    {
        await using var conn = _catalog.CreateConnection();
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync(MucCoLap);

        // Không tạo đơn mới chỉ để báo "không có trong giỏ".
        var gioId = await DocGioDangBanAsync(conn, tx, ThuNganId);
        if (gioId == null)
        {
            await tx.RollbackAsync();
            return "Sản phẩm không có trong giỏ.";
        }

        var hienCo = await DocSoLuongAsync(conn, tx, gioId.Value, productId);
        if (hienCo == null)
        {
            await tx.RollbackAsync();
            return "Sản phẩm không có trong giỏ.";
        }

        if (quantity <= 0)
        {
            await using var xoa = new MySqlCommand(
                "DELETE FROM ct_gio_hang WHERE gio_hang_id = @g AND san_pham_id = @p", conn, tx);
            xoa.Parameters.AddWithValue("@g", gioId.Value);
            xoa.Parameters.AddWithValue("@p", productId);
            await xoa.ExecuteNonQueryAsync();
            await tx.CommitAsync();
            return null;
        }

        var product = await _catalog.FindAsync(productId);
        if (product == null)
        {
            await tx.RollbackAsync();
            return "Sản phẩm không còn kinh doanh.";
        }

        if (quantity > product.Stock)
        {
            await tx.RollbackAsync();
            return $"Chỉ còn {product.Stock} sản phẩm.";
        }

        await CapNhatSoLuongAsync(conn, tx, gioId.Value, productId, quantity);
        await tx.CommitAsync();
        return null;
    }

    // ---------- Kiểm tra trước khi thanh toán ----------


    public async Task<string?> ValidateCartAsync(PosCart cart)
    {
        if (cart.Items.Count == 0)
            return "Giỏ hàng đang trống.";

        foreach (var item in cart.Items)
        {
            var product = await _catalog.FindAsync(
                item.ProductId);

            if (product == null)
                return $"Sản phẩm {item.Name} không còn kinh doanh.";

            if (item.Quantity <= 0)
                return $"Số lượng {item.Name} không hợp lệ.";

            if (item.Quantity > product.Stock)
                return $"Sản phẩm {item.Name} chỉ còn {product.Stock}.";

            if (product.Price <= 0 ||
                product.Price % 100 != 0)
                return $"Giá bán {item.Name} không hợp lệ.";

            if (item.UnitDiscount < 0 || item.UnitDiscount > item.UnitPrice)
                return $"Giảm giá của {item.Name} không hợp lệ.";

            if (item.UnitPrice != product.Price)
                return $"Giá sản phẩm {item.Name} đã thay đổi. " +
                       "Vui lòng cập nhật giỏ hàng.";
        }

        return null;
    }


    // ---------- E2: Đồng bộ khuyến mãi không còn hợp lệ ----------
    // Chỉ gọi sau khi giao dịch thanh toán đã kết thúc/rollback.
    // Trả về số dòng bị thay đổi. Controller phải yêu cầu thu ngân xác nhận lại.
    public async Task<int> RefreshInvalidPromotionsAsync()
    {
        await using var conn = _catalog.CreateConnection();
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync(MucCoLap);

        var gioId = await DocGioDangBanAsync(conn, tx, ThuNganId);
        if (gioId == null)
        {
            await tx.RollbackAsync();
            return 0;
        }

        // Khóa các dòng chi tiết giỏ hàng, rồi kiểm tra trạng thái KM.
        var invalidProducts = new List<int>();
        await using (var cmd = new MySqlCommand(
            """
            SELECT ct.san_pham_id, ct.khuyen_mai_id
            FROM ct_gio_hang ct
            WHERE ct.gio_hang_id = @g AND ct.khuyen_mai_id IS NOT NULL
            FOR UPDATE
            """, conn, tx))
        {
            cmd.Parameters.AddWithValue("@g", gioId.Value);
            await using var reader = await cmd.ExecuteReaderAsync();
            var promotions = new List<(int ProductId, int PromotionId)>();
            while (await reader.ReadAsync())
                promotions.Add((Convert.ToInt32(reader[0]), Convert.ToInt32(reader[1])));

            // Reader phải đóng trước khi thực hiện truy vấn khác trên cùng connection.
            await reader.CloseAsync();

            if (promotions.Count > 0)
            {
                var ids = promotions.Select(x => x.PromotionId).Distinct().ToArray();
                var names = ids.Select((_, i) => $"@km{i}").ToArray();
                await using var check = new MySqlCommand(
                    $"""
                    SELECT id
                    FROM v_chuong_trinh_khuyen_mai
                    WHERE id IN ({string.Join(",", names)})
                      AND trang_thai_hien_thi = 'DANG_CHAY'
                      AND (chi_thanh_vien = FALSE OR @coKhach = 1)
                    """, conn, tx);
                for (int i = 0; i < ids.Length; i++)
                    check.Parameters.AddWithValue(names[i], ids[i]);
                await using (var customerCmd = new MySqlCommand(
                    "SELECT khach_hang_id FROM gio_hang WHERE id = @g", conn, tx))
                {
                    customerCmd.Parameters.AddWithValue("@g", gioId.Value);
                    var customerId = await customerCmd.ExecuteScalarAsync();
                    check.Parameters.AddWithValue("@coKhach", customerId is not null and not DBNull ? 1 : 0);
                }

                var validIds = new HashSet<int>();
                await using (var validReader = await check.ExecuteReaderAsync())
                    while (await validReader.ReadAsync())
                        validIds.Add(Convert.ToInt32(validReader[0]));
                invalidProducts.AddRange(promotions
                    .Where(x => !validIds.Contains(x.PromotionId))
                    .Select(x => x.ProductId));
            }
        }

        int changed = 0;
        foreach (int productId in invalidProducts.Distinct())
        {
            await using var update = new MySqlCommand(
                """
                UPDATE ct_gio_hang
                SET khuyen_mai_id = NULL, giam_don_vi = 0
                WHERE gio_hang_id = @g AND san_pham_id = @p
                  AND khuyen_mai_id IS NOT NULL
                """, conn, tx);
            update.Parameters.AddWithValue("@g", gioId.Value);
            update.Parameters.AddWithValue("@p", productId);
            changed += await update.ExecuteNonQueryAsync();
        }
        await tx.CommitAsync();
        return changed;
    }

    // ---------- Hủy đơn đang bán ----------

    public async Task CancelAsync()
    {
        await using var conn = _catalog.CreateConnection();
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync(MucCoLap);

        var gioId = await DocGioDangBanAsync(conn, tx, ThuNganId);
        if (gioId == null)
        {
            await tx.RollbackAsync();
            return;
        }

        // ck_gio_huy bắt buộc có huy_luc, ly_do_huy, gia_tri_khi_huy khi DA_HUY.
        await using var huy = new MySqlCommand(
            """
            UPDATE gio_hang
            SET trang_thai = 'DA_HUY',
                huy_luc = NOW(),
                ly_do_huy = 'THU_NGAN_HUY',
                gia_tri_khi_huy = COALESCE((
                    SELECT CAST(SUM(so_luong * (don_gia - giam_don_vi)) AS UNSIGNED)
                    FROM ct_gio_hang WHERE gio_hang_id = @g), 0)
            WHERE id = @g AND trang_thai = 'DANG_BAN'
            """, conn, tx);
        huy.Parameters.AddWithValue("@g", gioId.Value);
        await huy.ExecuteNonQueryAsync();

        await tx.CommitAsync();
    }

    // =====================  HÀM NỘI BỘ  =====================

    /// <summary>Đơn DANG_BAN của thu ngân (khóa dòng để các request cùng thu ngân xếp hàng).</summary>
    private static async Task<int?> DocGioDangBanAsync(MySqlConnection conn, MySqlTransaction tx, int thuNganId)
    {
        await using var cmd = new MySqlCommand(
            "SELECT id FROM gio_hang WHERE thu_ngan_id = @t AND trang_thai = 'DANG_BAN' FOR UPDATE",
            conn, tx);
        cmd.Parameters.AddWithValue("@t", thuNganId);
        var kq = await cmd.ExecuteScalarAsync();
        return kq is null or DBNull ? null : Convert.ToInt32(kq);
    }

    private static async Task<int> LayHoacTaoGioAsync(MySqlConnection conn, MySqlTransaction tx, int thuNganId)
    {
        for (var lan = 0; lan < 3; lan++)
        {
            var id = await DocGioDangBanAsync(conn, tx, thuNganId);
            if (id != null)
                return id.Value;

            try
            {
                await using var tao = new MySqlCommand(
                    "INSERT INTO gio_hang (thu_ngan_id, trang_thai) VALUES (@t, 'DANG_BAN')", conn, tx);
                tao.Parameters.AddWithValue("@t", thuNganId);
                await tao.ExecuteNonQueryAsync();
                return (int)tao.LastInsertedId;
            }
            catch (MySqlException ex) when (ex.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
            {
                // uq_gio_dang_ban: request khác vừa tạo đơn cho thu ngân này.
                // Lỗi trùng khóa chỉ hủy câu lệnh, không hủy giao dịch -> vòng sau đọc lại thấy đơn đó.
            }
        }

        throw new InvalidOperationException("Không thể tạo đơn bán cho thu ngân.");
    }

    private static async Task<int?> DocSoLuongAsync(
        MySqlConnection conn, MySqlTransaction tx, int gioId, int productId)
    {
        await using var cmd = new MySqlCommand(
            "SELECT so_luong FROM ct_gio_hang WHERE gio_hang_id = @g AND san_pham_id = @p FOR UPDATE",
            conn, tx);
        cmd.Parameters.AddWithValue("@g", gioId);
        cmd.Parameters.AddWithValue("@p", productId);
        var kq = await cmd.ExecuteScalarAsync();
        return kq is null or DBNull ? null : Convert.ToInt32(kq);
    }

    private static async Task CapNhatSoLuongAsync(
        MySqlConnection conn, MySqlTransaction tx, int gioId, int productId, int soLuong)
    {
        await using var cmd = new MySqlCommand(
            "UPDATE ct_gio_hang SET so_luong = @q WHERE gio_hang_id = @g AND san_pham_id = @p", conn, tx);
        cmd.Parameters.AddWithValue("@q", soLuong);
        cmd.Parameters.AddWithValue("@g", gioId);
        cmd.Parameters.AddWithValue("@p", productId);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<PosCart> DocDongAsync(MySqlConnection conn, MySqlTransaction tx, int gioId)
    {
        var cart = new PosCart();

        await using var cmd = new MySqlCommand(
            """
            SELECT ct.san_pham_id, sp.ma, sp.ten_san_pham, ct.don_gia, ct.so_luong,
                   ct.khuyen_mai_id, ct.giam_don_vi
            FROM ct_gio_hang ct
            JOIN san_pham sp ON sp.id = ct.san_pham_id
            WHERE ct.gio_hang_id = @g
            ORDER BY ct.them_luc, ct.san_pham_id
            """, conn, tx);
        cmd.Parameters.AddWithValue("@g", gioId);

        await using var doc = await cmd.ExecuteReaderAsync();
        while (await doc.ReadAsync())
        {
            cart.Items.Add(new PosCartItem
            {
                ProductId = Convert.ToInt32(doc[0]),
                Code = Convert.ToString(doc[1]) ?? "",
                Name = Convert.ToString(doc[2]) ?? "",
                UnitPrice = Convert.ToInt64(doc[3]),
                Quantity = Convert.ToInt32(doc[4]),
                PromotionId = doc.IsDBNull(5) ? null : Convert.ToInt32(doc[5]),
                UnitDiscount = doc.IsDBNull(6) ? 0 : Convert.ToInt64(doc[6])
            });
        }

        return cart;
    }
}