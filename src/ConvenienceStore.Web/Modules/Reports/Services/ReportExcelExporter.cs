using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using ConvenienceStore.Web.Modules.Reports.Models;

namespace ConvenienceStore.Web.Modules.Reports.Services;

/// <summary>Creates a minimal standards-compliant .xlsx workbook without a third-party package.</summary>
public static class ReportExcelExporter
{
    private static readonly XNamespace Spreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace OfficeRelationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PackageRelationships = "http://schemas.openxmlformats.org/package/2006/relationships";
    private static readonly XNamespace ContentTypes = "http://schemas.openxmlformats.org/package/2006/content-types";

    public static byte[] Create(OperationalReportResponse report, string storeName, string exportedBy,
        DateTimeOffset exportedAt)
    {
        var rows = new List<IReadOnlyList<string>>();
        rows.Add(["Báo cáo", $"{report.Code} · {report.Title}"]);
        rows.Add(["Tên cửa hàng", storeName]);
        rows.Add(["Khoảng thời gian", $"{report.Range.From:dd/MM/yyyy} – {report.Range.To:dd/MM/yyyy} (UTC+7)"]);
        rows.Add(["Bộ lọc", DescribeFilters(report.Query)]);
        rows.Add(["Người xuất", exportedBy]);
        rows.Add(["Thời điểm xuất", exportedAt.ToOffset(ReportRangeResolver.VietnamOffset).ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture)]);
        rows.Add(Array.Empty<string>());
        rows.Add(["Chỉ số", "Giá trị"]);

        foreach (var metric in report.Metrics)
            rows.Add([metric.Label, metric.Format == "currency" ? metric.Value.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + " ₫" : metric.Value.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"))]);

        rows.Add([]);
        rows.Add(report.Columns);
        foreach (var row in report.Rows)
            rows.Add(row.Cells);

        var sheetData = new XElement(Spreadsheet + "sheetData");
        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var row = new XElement(Spreadsheet + "row", new XAttribute("r", rowIndex + 1));
            for (var columnIndex = 0; columnIndex < rows[rowIndex].Count; columnIndex++)
            {
                var cell = new XElement(Spreadsheet + "c",
                    new XAttribute("r", ColumnName(columnIndex + 1) + (rowIndex + 1)),
                    new XAttribute("t", "inlineStr"),
                    new XElement(Spreadsheet + "is",
                        new XElement(Spreadsheet + "t", new XAttribute(XNamespace.Xml + "space", "preserve"), Clean(rows[rowIndex][columnIndex] ?? string.Empty))));
                row.Add(cell);
            }
            sheetData.Add(row);
        }
        var sheet = new XDocument(new XElement(Spreadsheet + "worksheet", sheetData));

        var workbook = new XDocument(
            new XElement(Spreadsheet + "workbook",
                new XAttribute(XNamespace.Xmlns + "r", OfficeRelationships),
                new XElement(Spreadsheet + "sheets",
                    new XElement(Spreadsheet + "sheet",
                        new XAttribute("name", "Báo cáo"),
                        new XAttribute("sheetId", "1"),
                        new XAttribute(OfficeRelationships + "id", "rId1")))));

        var bytes = new MemoryStream();
        using (var archive = new ZipArchive(bytes, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteXml(archive, "[Content_Types].xml", new XDocument(
                new XElement(ContentTypes + "Types",
                    new XElement(ContentTypes + "Default", new XAttribute("Extension", "rels"), new XAttribute("ContentType", "application/vnd.openxmlformats-package.relationships+xml")),
                    new XElement(ContentTypes + "Default", new XAttribute("Extension", "xml"), new XAttribute("ContentType", "application/xml")),
                    new XElement(ContentTypes + "Override", new XAttribute("PartName", "/xl/workbook.xml"), new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml")),
                    new XElement(ContentTypes + "Override", new XAttribute("PartName", "/xl/worksheets/sheet1.xml"), new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml")))));
            WriteXml(archive, "_rels/.rels", new XDocument(
                new XElement(PackageRelationships + "Relationships",
                    new XElement(PackageRelationships + "Relationship",
                        new XAttribute("Id", "rId1"),
                        new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"),
                        new XAttribute("Target", "xl/workbook.xml")))));
            WriteXml(archive, "xl/workbook.xml", workbook);
            WriteXml(archive, "xl/_rels/workbook.xml.rels", new XDocument(
                new XElement(PackageRelationships + "Relationships",
                    new XElement(PackageRelationships + "Relationship",
                        new XAttribute("Id", "rId1"),
                        new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"),
                        new XAttribute("Target", "worksheets/sheet1.xml")))));
            WriteXml(archive, "xl/worksheets/sheet1.xml", sheet);
        }
        return bytes.ToArray();
    }

    private static void WriteXml(ZipArchive archive, string path, XDocument document)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        document.Save(writer, SaveOptions.DisableFormatting);
    }

    private static string DescribeFilters(ReportQuery query)
    {
        var filters = new List<string> { $"Kỳ={query.Period}" };
        if (query.From.HasValue) filters.Add($"Từ={query.From:dd/MM/yyyy}");
        if (query.To.HasValue) filters.Add($"Đến={query.To:dd/MM/yyyy}");
        if (query.CategoryId.HasValue) filters.Add($"Danh mục ID={query.CategoryId}");
        if (query.ProductId.HasValue) filters.Add($"Sản phẩm ID={query.ProductId}");
        if (query.CashierId.HasValue) filters.Add($"Thu ngân ID={query.CashierId}");
        if (query.GroupBy != "day") filters.Add($"Nhóm={query.GroupBy}");
        if (query.TopN != 10) filters.Add($"Top={query.TopN}");
        if (query.SortBy != "quantity") filters.Add($"Sắp xếp={query.SortBy}");
        if (query.StockStatus != "all") filters.Add($"Trạng thái tồn={query.StockStatus}");
        return string.Join("; ", filters);
    }

    private static string ColumnName(int index)
    {
        var result = string.Empty;
        while (index > 0)
        {
            index--;
            result = (char)('A' + index % 26) + result;
            index /= 26;
        }
        return result;
    }

    private static string Clean(string value) => new(value.Where(c => !char.IsControl(c) || c is '\t' or '\n' or '\r').ToArray());
}
