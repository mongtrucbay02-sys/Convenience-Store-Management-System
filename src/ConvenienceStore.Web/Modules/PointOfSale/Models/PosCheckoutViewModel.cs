
namespace ConvenienceStore.Web.Models.Pos
{
    public sealed class PosCheckoutViewModel
    {
        // POS-05: Tổng tiền hàng trước giảm giá
        public long Subtotal { get; set; }

        // Số tiền giảm giá từ POS-04
        public long Discount { get; set; }

        // Tổng số tiền khách cần thanh toán
        public long Total { get; set; }

        // Giá trị người dùng nhập trên giao diện
        public string CashInput { get; set; } = "";

        // Số tiền mặt khách đã đưa
        public long? CashReceived { get; set; }

        // Số tiền thừa trả lại khách
        public long? Change { get; set; }

        // Thông báo lỗi khi nhập tiền không hợp lệ
        // hoặc khách đưa không đủ tiền
        public string? Error { get; set; }

        // Thông báo khi xác nhận tiền mặt
        // và gọi POS-07 tạm thời
        public string? Message { get; set; }
    }
}
