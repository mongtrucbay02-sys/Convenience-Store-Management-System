
using ConvenienceStore.Web.Models.Pos;

namespace ConvenienceStore.Web.Services.Pos;

public sealed class PosPaymentService
{
    public Task<string> PrepareAsync(
        PosCart cart,
        long cashReceived,
        long change)
    {
        // POS-07 stub - tuần 4
        // Chưa lưu hóa đơn.
        // Chưa trừ tồn kho.
        // Chưa ghi nhận thanh toán thực tế.
        // Chưa xóa giỏ hàng.

        return Task.FromResult(
            "Đã xác nhận dữ liệu thanh toán tiền mặt. " +
            "POS-07 đang là hàm tạm, " +
            "đơn hàng chưa được hoàn tất.");
    }
}
