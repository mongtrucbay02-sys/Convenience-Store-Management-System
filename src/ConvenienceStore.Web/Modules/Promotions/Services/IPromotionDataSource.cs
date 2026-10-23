using ConvenienceStore.Web.Modules.Promotions.Models;

namespace ConvenienceStore.Web.Modules.Promotions.Services;

/// <summary>Chương trình khuyến mãi như được lưu (chưa suy ra trạng thái hiển thị).</summary>
public sealed record PromotionRecord(
    long Id,
    string Code,
    string Name,
    string? Description,
    DiscountType DiscountType,
    long DiscountValue,
    long? MaxDiscountAmount,
    IReadOnlyList<ProductRefDto> Products,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    bool MembersOnly,
    PromotionStatus Status,
    int UsageCount);

/// <summary>
/// Nguồn dữ liệu khuyến mãi. Hiện là dữ liệu mẫu (chưa có bảng, chờ ERD/migration được duyệt ở #1);
/// thay bằng bản dùng ApplicationDbContext mà không phải đổi PromotionService.
/// </summary>
public interface IPromotionDataSource
{
    Task<IReadOnlyList<PromotionRecord>> GetAllAsync(CancellationToken cancellationToken);
}
