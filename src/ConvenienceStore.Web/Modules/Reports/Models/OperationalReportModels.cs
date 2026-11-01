namespace ConvenienceStore.Web.Modules.Reports.Models;

/// <summary>Common date and dimension filters for BC-02 through BC-06.</summary>
public sealed class ReportQuery
{
    public string Period { get; set; } = "thisMonth";
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public string GroupBy { get; set; } = "day";
    public int? CategoryId { get; set; }
    public long? ProductId { get; set; }
    public long? CashierId { get; set; }
    public int? SupplierId { get; set; }
    public int TopN { get; set; } = 10;
    public string SortBy { get; set; } = "quantity";
    public string StockStatus { get; set; } = "all";
}

public sealed record ResolvedReportRange(
    DateOnly From,
    DateOnly To,
    DateTimeOffset FromInclusive,
    DateTimeOffset ToExclusive,
    string Period,
    string GroupBy);

public sealed record ReportMetric(string Label, long Value, string Format = "currency");
public sealed record ReportChartPoint(string Label, long Value, long? SecondaryValue = null);
public sealed record ReportTableRow(string Key, IReadOnlyList<string> Cells);

public sealed record OperationalReportResponse(
    string Code,
    string Title,
    ResolvedReportRange Range,
    IReadOnlyList<ReportMetric> Metrics,
    IReadOnlyList<string> Columns,
    IReadOnlyList<ReportTableRow> Rows,
    IReadOnlyList<ReportChartPoint> Chart,
    string? Message,
    ReportQuery Query);
