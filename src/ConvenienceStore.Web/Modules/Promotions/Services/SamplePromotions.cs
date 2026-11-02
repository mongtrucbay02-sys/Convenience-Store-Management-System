using ConvenienceStore.Web.Modules.Promotions.Models;

namespace ConvenienceStore.Web.Modules.Promotions.Services;

/// <summary>Dữ liệu mẫu ban đầu. Số lượt dùng là 0 vì chưa có hóa đơn (POS-07, tuần 5).</summary>
public static class SamplePromotions
{
    public static IReadOnlyList<PromotionRecord> Create(DateTimeOffset utcNow)
    {
        var now = utcNow.ToOffset(TimeSpan.FromHours(7));
        var day = new DateTimeOffset(now.Date, now.Offset);
        var p = SampleProductLookup.All;
        const string img = "https://placehold.co/96x96/png?text=";

        return new[]
        {
            new PromotionRecord(1, "KM-0001", "Giảm 10% sữa tươi cuối tuần", "Áp dụng thứ 7, chủ nhật",
                DiscountType.Percent, 10, 5_000, new[] { p[0] },
                day.AddDays(-2), day.AddDays(5).AddSeconds(-1), true, PromotionStatus.Active, 0, img + "Sua"),
            new PromotionRecord(2, "KM-0002", "Nước suối giảm 1.000đ", null,
                DiscountType.FixedAmount, 1_000, null, new[] { p[1] },
                day.AddDays(-10), day.AddDays(20).AddSeconds(-1), false, PromotionStatus.Active, 0, img + "Nuoc"),
            new PromotionRecord(3, "KM-0003", "Combo mì và cà phê", "Chương trình sắp tới",
                DiscountType.Percent, 15, 8_000, new[] { p[2], p[3] },
                day.AddDays(3), day.AddDays(10).AddSeconds(-1), false, PromotionStatus.Active, 0),
            new PromotionRecord(4, "KM-0004", "Bánh mì sandwich mùa lễ", "Chưa kích hoạt",
                DiscountType.FixedAmount, 2_000, null, new[] { p[4] },
                day.AddDays(7), day.AddDays(14).AddSeconds(-1), true, PromotionStatus.Draft, 0, img + "Banh"),
            new PromotionRecord(5, "KM-0005", "Cà phê lon giảm 5%", "Đang tạm dừng",
                DiscountType.Percent, 5, null, new[] { p[3] },
                day.AddDays(-5), day.AddDays(25).AddSeconds(-1), false, PromotionStatus.Paused, 0),
            // Đã kích hoạt nhưng quá hạn: tự hiển thị "Đã kết thúc" (PA-KM-05.A).
            new PromotionRecord(6, "KM-0006", "Khai trương giảm 20% mì gói", null,
                DiscountType.Percent, 20, 10_000, new[] { p[2] },
                day.AddDays(-30), day.AddDays(-3).AddSeconds(-1), false, PromotionStatus.Active, 0),
        };
    }
}
