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
    int UsageCount,
    string? ImageUrl = null);

/// <summary>
/// Nơi lưu khuyến mãi. Hiện là bản lưu trong bộ nhớ (chưa có bảng, chờ ERD/migration được duyệt ở #1,
/// dữ liệu mất khi khởi động lại); thay bằng bản dùng ApplicationDbContext mà không phải đổi PromotionService.
/// </summary>
public interface IPromotionDataSource
{
    Task<IReadOnlyList<PromotionRecord>> GetAllAsync(CancellationToken cancellationToken);

    Task<PromotionRecord?> GetByIdAsync(long id, CancellationToken cancellationToken);

    /// <summary>Lưu chương trình mới; nguồn dữ liệu gán Id và Code (do hệ thống sinh).</summary>
    Task<PromotionRecord> CreateAsync(PromotionRecord record, CancellationToken cancellationToken);

    /// <summary>Ghi đè chương trình theo Id. Trả false nếu không tồn tại.</summary>
    Task<bool> UpdateAsync(PromotionRecord record, CancellationToken cancellationToken);
}
