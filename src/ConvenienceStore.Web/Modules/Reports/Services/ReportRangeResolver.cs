using ConvenienceStore.Web.Modules.Reports.Models;

namespace ConvenienceStore.Web.Modules.Reports.Services;

public static class ReportRangeResolver
{
    public static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

    public static bool TryResolve(ReportQuery query, DateTimeOffset now, out ResolvedReportRange? range, out string? error)
    {
        var today = DateOnly.FromDateTime(now.ToOffset(VietnamOffset).DateTime);
        var period = (query.Period ?? "thisMonth").Trim().ToLowerInvariant();
        var from = period switch
        {
            "today" => today,
            "last7days" or "7days" => today.AddDays(-6),
            "thismonth" => new DateOnly(today.Year, today.Month, 1),
            "lastmonth" => new DateOnly(today.Year, today.Month, 1).AddMonths(-1),
            "custom" => query.From ?? new DateOnly(today.Year, today.Month, 1),
            _ => new DateOnly(today.Year, today.Month, 1)
        };
        var to = period switch
        {
            "today" or "last7days" or "7days" or "thismonth" => today,
            "lastmonth" => new DateOnly(today.Year, today.Month, 1).AddDays(-1),
            "custom" => query.To ?? today,
            _ => today
        };

        // Explicit from/to also makes ad-hoc API calls predictable.
        if (query.From.HasValue || query.To.HasValue)
        {
            from = query.From ?? from;
            to = query.To ?? to;
            period = "custom";
        }

        if (from > to)
        {
            range = null;
            error = "Ngày bắt đầu phải trước hoặc bằng ngày kết thúc.";
            return false;
        }
        if (to.DayNumber - from.DayNumber + 1 > 366)
        {
            range = null;
            error = "Khoảng thời gian không được dài hơn 366 ngày.";
            return false;
        }

        var groupBy = (query.GroupBy ?? "day").Trim().ToLowerInvariant();
        if (groupBy is not ("day" or "week" or "month")) groupBy = "day";
        range = new ResolvedReportRange(
            from, to,
            new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), VietnamOffset),
            new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), VietnamOffset),
            period, groupBy);
        error = null;
        return true;
    }

    public static DateOnly Bucket(DateOnly date, string groupBy) => groupBy switch
    {
        "week" => date.AddDays(-(((int)date.DayOfWeek + 6) % 7)),
        "month" => new DateOnly(date.Year, date.Month, 1),
        _ => date
    };
}
