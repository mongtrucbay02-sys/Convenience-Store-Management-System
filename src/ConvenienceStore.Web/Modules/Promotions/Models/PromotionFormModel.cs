namespace ConvenienceStore.Web.Modules.Promotions.Models;

/// <summary>Dữ liệu form Tạo/Sửa khuyến mãi. Thời gian nhập theo giờ Việt Nam (UTC+7).</summary>
public sealed class PromotionFormModel
{
    public long? Id { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public DiscountType? DiscountType { get; set; } = Models.DiscountType.Percent;
    public long? DiscountValue { get; set; }
    public long? MaxDiscountAmount { get; set; }
    public List<long> ProductIds { get; set; } = new();
    public DateTime? StartAt { get; set; }
    public DateTime? EndAt { get; set; }
    public bool MembersOnly { get; set; }
    public string? ImageUrl { get; set; }
    public int UsageCount { get; set; }
}
