
using System;
using System.Linq;
using System.Threading.Tasks;
using MySqlConnector;

namespace ConvenienceStore.Web.Services.Pos
{
    /// <summary>
    /// POS-09: Hủy đơn hàng đang bán.
    /// BR-POS-10: Lưu lịch sử hủy đơn để đối soát.
    /// </summary>
    public sealed class PosCancellationService
    {
        private readonly MySqlPosCatalog _catalog;
        private readonly PosCartService _cart;

        public PosCancellationService(
            MySqlPosCatalog catalog,
            PosCartService cart)
        {
            _catalog = catalog;
            _cart = cart;
        }

        // ==========================================
        // POS-09: HỦY ĐƠN HÀNG ĐANG BÁN
        // ==========================================

        public async Task<bool> CancelCurrentAsync(
            string cashierId,
            string source = "POS")
        {
            // 1. Kiểm tra mã thu ngân
            if (string.IsNullOrWhiteSpace(cashierId))
            {
                throw new ArgumentException(
                    "Không xác định được người hủy đơn.",
                    nameof(cashierId));
            }

            // 2. Kiểm tra nguồn hủy
            if (source != "POS" && source != "LOGOUT")
            {
                throw new ArgumentException(
                    "Nguồn hủy đơn không hợp lệ.",
                    nameof(source));
            }

            // 3. Lấy giỏ hàng hiện tại từ Session
            var cart = _cart.GetCart();

            // Không ghi lịch sử nếu giỏ hàng trống
            if (cart.Items == null || !cart.Items.Any())
            {
                return false;
            }

            // 4. Lấy giá trị đơn trước khi hủy
            long orderValue = cart.Total;

            // 5. Tính tổng số lượng sản phẩm
            int itemCount = cart.Items.Sum(
                item => item.Quantity);

            // Kiểm tra dữ liệu trước khi lưu
            if (orderValue < 0 || itemCount <= 0)
            {
                throw new InvalidOperationException(
                    "Dữ liệu đơn hàng không hợp lệ.");
            }

            // ======================================
            // 6. GHI LỊCH SỬ HỦY ĐƠN VÀO MYSQL
            // ======================================

            const string sql = """
                INSERT INTO pos_cancelled_orders
                (
                    cashier_id,
                    cancelled_at_utc,
                    order_value,
                    item_count,
                    source
                )
                VALUES
                (
                    @cashierId,
                    UTC_TIMESTAMP(6),
                    @orderValue,
                    @itemCount,
                    @source
                );
                """;

            await using var connection =
                _catalog.CreateConnection();

            await connection.OpenAsync();

            await using var command =
                new MySqlCommand(sql, connection);

            command.Parameters.AddWithValue(
                "@cashierId", cashierId.Trim());

            command.Parameters.AddWithValue(
                "@orderValue", orderValue);

            command.Parameters.AddWithValue(
                "@itemCount", itemCount);

            command.Parameters.AddWithValue(
                "@source", source);

            int affectedRows =
                await command.ExecuteNonQueryAsync();

            if (affectedRows != 1)
            {
                throw new InvalidOperationException(
                    "Không thể ghi lịch sử hủy đơn.");
            }

            // ======================================
            // 7. XÓA GIỎ SAU KHI LƯU THÀNH CÔNG
            // ======================================

            _cart.Cancel();

            // Trả về true: Hủy đơn thành công
            return true;
        }
    }
}
