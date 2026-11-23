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
        var changed = await ApplyPromotionsAsync(conn, tx, gioId);
        var cart = await DocDongAsync(conn, tx, gioId);
        cart.PromotionsChanged = changed > 0;

        await tx.CommitAsync();
        return cart;
    }

    // POS-03: mutate only the authenticated cashier's existing active cart.
    public async Task<string?> SetCustomerAsync(int? customerId)
    {
        if (customerId is <= 0) return "Khách hàng không hợp lệ.";
        await using var conn = _catalog.CreateConnection();
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync(MucCoLap);
        var cartId = await DocGioDangBanAsync(conn, tx, ThuNganId);
        if (cartId == null) return "Không có đơn đang bán. Hãy mở quầy bán hàng trước.";
        if (customerId.HasValue)
        {
            await using var customer = new MySqlCommand(
                "SELECT id FROM khach_hang WHERE id=@id AND trang_thai='HOAT_DONG' FOR UPDATE", conn, tx);
            customer.Parameters.AddWithValue("@id", customerId.Value);
            if (await customer.ExecuteScalarAsync() == null) return "Khách hàng không còn hoạt động.";
        }
        await using var update = new MySqlCommand(
            "UPDATE gio_hang SET khach_hang_id=@id WHERE id=@g AND trang_thai='DANG_BAN'", conn, tx);
        update.Parameters.AddWithValue("@id", (object?)customerId ?? DBNull.Value);
        update.Parameters.AddWithValue("@g", cartId.Value);
        await update.ExecuteNonQueryAsync();
        await ApplyPromotionsAsync(conn, tx, cartId.Value);
        await tx.CommitAsync();
        return null;
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

        int changed = await ApplyPromotionsAsync(conn, tx, gioId.Value);
        await tx.CommitAsync();
        return changed;
    }

    private static async Task<int> ApplyPromotionsAsync(MySqlConnection conn, MySqlTransaction tx, int gioId)
    {
        var promotions = await PosPromotionCalculator.ReadAsync(conn, tx);
        await using var customer = new MySqlCommand("""
            SELECT COUNT(*) FROM gio_hang g JOIN khach_hang k ON k.id = g.khach_hang_id
            WHERE g.id = @g AND k.trang_thai = 'HOAT_DONG'
            """, conn, tx);
        customer.Parameters.AddWithValue("@g", gioId);
        bool member = Convert.ToInt32(await customer.ExecuteScalarAsync()) > 0;
        var cart = await DocDongAsync(conn, tx, gioId);
        int changed = 0;
        foreach (var item in cart.Items)
        {
            var best = PosPromotionCalculator.Best(promotions, item.ProductId, item.UnitPrice, member);
            long discount = best is null ? 0 : PosPromotionCalculator.Discount(item.UnitPrice, best);
            if (item.PromotionId == best?.Id && item.UnitDiscount == discount) continue;
            await using var update = new MySqlCommand("""
                UPDATE ct_gio_hang SET khuyen_mai_id = @km, giam_don_vi = @d
                WHERE gio_hang_id = @g AND san_pham_id = @p
                """, conn, tx);
            update.Parameters.AddWithValue("@km", (object?)best?.Id ?? DBNull.Value);
            update.Parameters.AddWithValue("@d", discount);
            update.Parameters.AddWithValue("@g", gioId);
            update.Parameters.AddWithValue("@p", item.ProductId);
            changed += await update.ExecuteNonQueryAsync();
        }
        return changed;
    }

    // ---------- Hủy đơn đang bán ----------

    public async Task CancelAsync()
    {
        await using var conn = _catalog.CreateConnection();
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync(MucCoLap);

        await HuyGioDangBan.HuyAsync(conn, tx, ThuNganId);

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
        await using (var customer = new MySqlCommand("""
            SELECT k.id,k.ho_ten,k.diem_hien_co FROM gio_hang g
            JOIN khach_hang k ON k.id=g.khach_hang_id WHERE g.id=@g
            """, conn, tx))
        {
            customer.Parameters.AddWithValue("@g", gioId);
            await using var reader = await customer.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                cart.CustomerId = Convert.ToInt32(reader[0]);
                cart.CustomerName = reader.IsDBNull(1) ? "Khách đã ẩn" : reader.GetString(1);
                cart.CustomerPoints = Convert.ToUInt32(reader[2]);
            }
        }

        await using var cmd = new MySqlCommand(
            """
            SELECT ct.san_pham_id, sp.ma, sp.ten_san_pham, ct.don_gia, ct.so_luong,
                   ct.khuyen_mai_id, ct.giam_don_vi, km.ten
            FROM ct_gio_hang ct
            JOIN san_pham sp ON sp.id = ct.san_pham_id
            LEFT JOIN chuong_trinh_khuyen_mai km ON km.id = ct.khuyen_mai_id
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
                UnitDiscount = doc.IsDBNull(6) ? 0 : Convert.ToInt64(doc[6]),
                PromotionName = doc.IsDBNull(7) ? "" : doc.GetString(7)
            });
        }

        return cart;
    }
}
