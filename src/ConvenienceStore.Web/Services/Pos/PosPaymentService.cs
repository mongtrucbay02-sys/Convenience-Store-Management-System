
using ConvenienceStore.Web.Models.Pos;

namespace ConvenienceStore.Web.Services.Pos;

/// <summary>
/// POS-05: Kiểm tra và chuẩn bị thanh toán.
/// POS-07: Sẽ tích hợp với dịch vụ hoàn tất đơn.
/// </summary>
public sealed class PosPaymentService
{
    /// <summary>
    /// Kiểm tra thanh toán tiền mặt.
    /// Giữ nguyên chữ ký hàm để tương thích PosController.
    /// </summary>
    public Task<string> PrepareAsync(
        PosCart cart,
        long cashReceived,
        long change)
    {
        if (cart == null)
        {
            return Task.FromResult(
                "Lỗi: Không tìm thấy giỏ hàng.");
        }

        if (cart.Items == null || !cart.Items.Any())
        {
            return Task.FromResult(
                "Lỗi: Giỏ hàng đang trống.");
        }

        if (cart.Total <= 0)
        {
            return Task.FromResult(
                "Lỗi: Tổng tiền thanh toán không hợp lệ.");
        }

        if (cashReceived < 0)
        {
            return Task.FromResult(
                "Lỗi: Tiền khách đưa không được âm.");
        }

        if (cashReceived < cart.Total)
        {
            long missing = cart.Total - cashReceived;

            return Task.FromResult(
                $"Khách thanh toán chưa đủ. " +
                $"Còn thiếu {missing:N0} đ.");
        }

        long actualChange = cashReceived - cart.Total;

        if (change != actualChange)
        {
            return Task.FromResult(
                "Lỗi: Số tiền thừa không khớp.");
        }

        // POS-05 chỉ xác nhận số tiền.
        // Chưa lưu hóa đơn hoặc trừ kho tại đây.
        return Task.FromResult(
            "Đã xác nhận thanh toán tiền mặt hợp lệ. " +
            $"Tổng tiền: {cart.Total:N0} đ. " +
            $"Khách đưa: {cashReceived:N0} đ. " +
            $"Tiền thừa: {actualChange:N0} đ. " +
            "Đơn hàng chưa được hoàn tất.");
    }
}
