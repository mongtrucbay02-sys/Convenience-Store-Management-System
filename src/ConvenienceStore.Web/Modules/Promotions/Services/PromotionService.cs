using ConvenienceStore.Web.Modules.Promotions.Models;

namespace ConvenienceStore.Web.Modules.Promotions.Services;

public sealed class PromotionService : IPromotionService
{
    // Ngày theo giờ Việt Nam (UTC+7), cùng quy ước với module báo cáo (BR-BC-03).
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

    private readonly IPromotionDataSource _dataSource;
    private readonly TimeProvider _timeProvider;

    public PromotionService(IPromotionDataSource dataSource, TimeProvider timeProvider)
    {
        _dataSource = dataSource;
        _timeProvider = timeProvider;
    }

    public async Task<PromotionListResponse> ListAsync(
        PromotionQuery query, bool isAdmin, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var records = await _dataSource.GetAllAsync(cancellationToken);

        var items = records
            .Select(r => ToDto(r, now))
            .Where(p => MatchesStatus(p, query.DisplayStatus, isAdmin))
            .Where(p => MatchesPeriod(p, query.From, query.To))
            .OrderByDescending(p => p.StartAt)
            .ThenBy(p => p.Code, StringComparer.Ordinal)
            .ToList();

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var totalPages = (int)Math.Ceiling(items.Count / (double)pageSize);

        return new PromotionListResponse(
            page,
            pageSize,
            items.Count,
            totalPages,
            items.Skip((page - 1) * pageSize).Take(pageSize).ToList());
    }

    /// <summary>BR-KM-07, PA-KM-05.A: suy ra trạng thái hiển thị khi truy vấn, không dùng tác vụ nền.</summary>
    public static PromotionDisplayStatus GetDisplayStatus(
        PromotionStatus status, DateTimeOffset startAt, DateTimeOffset endAt, DateTimeOffset now) =>
        status switch
        {
            PromotionStatus.Draft => PromotionDisplayStatus.Draft,
            PromotionStatus.Paused => PromotionDisplayStatus.Paused,
            PromotionStatus.Ended => PromotionDisplayStatus.Ended,
            _ when now < startAt => PromotionDisplayStatus.Scheduled,
            _ when now <= endAt => PromotionDisplayStatus.Running,
            _ => PromotionDisplayStatus.Ended,
        };

    private static PromotionDto ToDto(PromotionRecord r, DateTimeOffset now) => new(
        r.Id, r.Code, r.Name, r.Description, r.DiscountType, r.DiscountValue, r.MaxDiscountAmount,
        r.Products, r.StartAt, r.EndAt, r.MembersOnly, r.Status,
        GetDisplayStatus(r.Status, r.StartAt, r.EndAt, now),
        r.UsageCount);

    // TN luôn chỉ thấy chương trình đang chạy, bất kể bộ lọc gửi lên.
    private static bool MatchesStatus(PromotionDto p, PromotionDisplayStatus? filter, bool isAdmin) =>
        !isAdmin
            ? p.DisplayStatus == PromotionDisplayStatus.Running
            : filter is null || p.DisplayStatus == filter;

    // Lấy chương trình có thời gian hiệu lực giao với [from, to] (cả ngày to).
    private static bool MatchesPeriod(PromotionDto p, DateOnly? from, DateOnly? to)
    {
        if (from is { } f && p.EndAt < StartOfDay(f)) return false;
        if (to is { } t && p.StartAt >= StartOfDay(t.AddDays(1))) return false;
        return true;
    }

    private static DateTimeOffset StartOfDay(DateOnly d) =>
        new(d.ToDateTime(TimeOnly.MinValue), VietnamOffset);
}
