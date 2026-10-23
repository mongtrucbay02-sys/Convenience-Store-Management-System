using ConvenienceStore.Web.Modules.Promotions.Models;

namespace ConvenienceStore.Web.Modules.Promotions.Services;

/// <summary>Dữ liệu mẫu tuần 4. Số lượt dùng luôn 0 vì chưa có hóa đơn (POS-07, tuần 5).</summary>
public sealed class SamplePromotionDataSource : IPromotionDataSource
{
    private static readonly ProductRefDto Milk = new(101, "SUA-1L", "Sữa tươi 1L");
    private static readonly ProductRefDto Water = new(102, "NUOC-500", "Nước suối 500ml");
    private static readonly ProductRefDto Noodle = new(103, "MI-GOI", "Mì gói");
    private static readonly ProductRefDto Coffee = new(104, "CF-LON", "Cà phê lon");
    private static readonly ProductRefDto Bread = new(105, "BANH-MI", "Bánh mì sandwich");

    private readonly TimeProvider _timeProvider;

    public SamplePromotionDataSource(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public Task<IReadOnlyList<PromotionRecord>> GetAllAsync(CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow().ToOffset(TimeSpan.FromHours(7));
        var day = new DateTimeOffset(now.Date, now.Offset);

        IReadOnlyList<PromotionRecord> all = new[]
        {
            new PromotionRecord(1, "KM-0001", "Giảm 10% sữa tươi cuối tuần", "Áp dụng thứ 7, chủ nhật",
                DiscountType.Percent, 10, 5_000, new[] { Milk },
                day.AddDays(-2), day.AddDays(5).AddSeconds(-1), true, PromotionStatus.Active, 0),
            new PromotionRecord(2, "KM-0002", "Nước suối giảm 1.000đ", null,
                DiscountType.FixedAmount, 1_000, null, new[] { Water },
                day.AddDays(-10), day.AddDays(20).AddSeconds(-1), false, PromotionStatus.Active, 0),
            new PromotionRecord(3, "KM-0003", "Combo mì và cà phê", "Chương trình sắp tới",
                DiscountType.Percent, 15, 8_000, new[] { Noodle, Coffee },
                day.AddDays(3), day.AddDays(10).AddSeconds(-1), false, PromotionStatus.Active, 0),
            new PromotionRecord(4, "KM-0004", "Bánh mì sandwich mùa lễ", "Chưa kích hoạt",
                DiscountType.FixedAmount, 2_000, null, new[] { Bread },
                day.AddDays(7), day.AddDays(14).AddSeconds(-1), true, PromotionStatus.Draft, 0),
            new PromotionRecord(5, "KM-0005", "Cà phê lon giảm 5%", "Đang tạm dừng",
                DiscountType.Percent, 5, null, new[] { Coffee },
                day.AddDays(-5), day.AddDays(25).AddSeconds(-1), false, PromotionStatus.Paused, 0),
            // Đã kích hoạt nhưng quá hạn: tự hiển thị "Đã kết thúc" (PA-KM-05.A).
            new PromotionRecord(6, "KM-0006", "Khai trương giảm 20% mì gói", null,
                DiscountType.Percent, 20, 10_000, new[] { Noodle },
                day.AddDays(-30), day.AddDays(-3).AddSeconds(-1), false, PromotionStatus.Active, 0),
        };
        return Task.FromResult(all);
    }
}
