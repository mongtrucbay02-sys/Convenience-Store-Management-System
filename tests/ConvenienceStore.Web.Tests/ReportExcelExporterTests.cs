using System.IO.Compression;
using System.Xml.Linq;
using ConvenienceStore.Web.Modules.Reports.Models;
using ConvenienceStore.Web.Modules.Reports.Services;

namespace ConvenienceStore.Web.Tests;

public sealed class ReportExcelExporterTests
{
    [Fact]
    public void Create_ReturnsExcelPackageWithMetadataMetricsAndTotalRow()
    {
        var offset = TimeSpan.FromHours(7);
        var from = new DateOnly(2026, 11, 1);
        var to = new DateOnly(2026, 11, 2);
        var report = new OperationalReportResponse(
            "BC-02", "Doanh thu theo thời gian",
            new ResolvedReportRange(from, to, new DateTimeOffset(2026, 11, 1, 0, 0, 0, offset),
                new DateTimeOffset(2026, 11, 3, 0, 0, 0, offset), "custom", "day"),
            [new ReportMetric("Doanh thu thuần", 120_000)],
            ["Ngày", "Doanh thu"],
            [new ReportTableRow("total", ["Tổng cộng", "120.000 ₫"])],
            [new ReportChartPoint("01/11", 120_000)], null,
            new ReportQuery { Period = "custom", From = from, To = to });

        var bytes = ReportExcelExporter.Create(report, "Cửa hàng tiện lợi", "Admin", DateTimeOffset.UtcNow);
        using var stream = new MemoryStream(bytes);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        Assert.Contains(archive.Entries, x => x.FullName == "[Content_Types].xml");
        Assert.Contains(archive.Entries, x => x.FullName == "xl/workbook.xml");
        var sheetEntry = Assert.Single(archive.Entries, x => x.FullName == "xl/worksheets/sheet1.xml");
        using var sheet = sheetEntry.Open();
        var xml = XDocument.Load(sheet);
        var text = string.Concat(xml.DescendantNodes().OfType<XText>().Select(x => x.Value));
        Assert.Contains("Cửa hàng tiện lợi", text);
        Assert.Contains("Doanh thu thuần", text);
        Assert.Contains("Tổng cộng", text);
    }
}
