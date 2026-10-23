using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConvenienceStore.Web.Modules.Promotions.Models;

/// <summary>Serialize enum thành chuỗi UPPER_SNAKE (PERCENT, FIXED_AMOUNT...) theo hợp đồng API.</summary>
public sealed class UpperSnakeEnumConverter<T>()
    : JsonStringEnumConverter<T>(JsonNamingPolicy.SnakeCaseUpper) where T : struct, Enum;

[JsonConverter(typeof(UpperSnakeEnumConverter<DiscountType>))]
public enum DiscountType { Percent, FixedAmount }

/// <summary>Trạng thái lưu.</summary>
[JsonConverter(typeof(UpperSnakeEnumConverter<PromotionStatus>))]
public enum PromotionStatus { Draft, Active, Paused, Ended }

/// <summary>Trạng thái hiển thị, suy ra khi truy vấn (BR-KM-07, PA-KM-05.A).</summary>
[JsonConverter(typeof(UpperSnakeEnumConverter<PromotionDisplayStatus>))]
public enum PromotionDisplayStatus { Draft, Scheduled, Running, Paused, Ended }

public sealed record ProductRefDto(long Id, string Sku, string Name);

public sealed record PromotionDto(
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
    PromotionDisplayStatus DisplayStatus,
    int UsageCount,
    string? ImageUrl);

public sealed record PromotionListResponse(
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages,
    IReadOnlyList<PromotionDto> Items);

public sealed record PromotionQuery(
    PromotionDisplayStatus? DisplayStatus,
    DateOnly? From,
    DateOnly? To,
    int Page,
    int PageSize);

/// <summary>KM-02. Các trường nullable để thiếu trường trả lỗi kiểm tra dữ liệu (400) thay vì lỗi bind.</summary>
public sealed record CreatePromotionRequest(
    string? Name,
    string? Description,
    DiscountType? DiscountType,
    long? DiscountValue,
    long? MaxDiscountAmount,
    IReadOnlyList<long>? ProductIds,
    DateTimeOffset? StartAt,
    DateTimeOffset? EndAt,
    bool MembersOnly = false,
    bool Activate = false,
    string? ImageUrl = null);

/// <summary>
/// KM-03. Chỉ gửi trường cần đổi (trường null là không đổi). Chuỗi rỗng ở description/imageUrl là xóa giá trị;
/// maxDiscountAmount = 0 là bỏ mức giảm tối đa.
/// </summary>
public sealed record UpdatePromotionRequest(
    string? Name,
    string? Description,
    DiscountType? DiscountType,
    long? DiscountValue,
    long? MaxDiscountAmount,
    IReadOnlyList<long>? ProductIds,
    DateTimeOffset? StartAt,
    DateTimeOffset? EndAt,
    bool? MembersOnly,
    string? ImageUrl);
