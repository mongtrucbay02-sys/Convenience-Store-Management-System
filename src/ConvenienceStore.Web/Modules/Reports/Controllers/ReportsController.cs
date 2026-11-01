using ConvenienceStore.Web.Modules.Reports.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Modules.Reports.Controllers;

[Area("Reports")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public class ReportsController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly IOperationalReportsService _reportsService;
    private readonly TimeProvider _clock;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IDashboardService dashboardService, IOperationalReportsService reportsService,
        TimeProvider clock, IConfiguration configuration, ILogger<ReportsController> logger)
    {
        _dashboardService = dashboardService;
        _reportsService = reportsService;
        _clock = clock;
        _configuration = configuration;
        _logger = logger;
    }

    // BC-01: /Reports/Reports/Dashboard
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken) =>
        View(await _dashboardService.GetDashboardAsync(cancellationToken));

    public Task<IActionResult> Revenue([FromQuery] Models.ReportQuery query, CancellationToken ct) =>
        Render(_reportsService.GetRevenueAsync(query, ct));

    public Task<IActionResult> Products([FromQuery] Models.ReportQuery query, CancellationToken ct) =>
        Render(_reportsService.GetProductsAsync(query, ct));

    public Task<IActionResult> Categories([FromQuery] Models.ReportQuery query, CancellationToken ct) =>
        Render(_reportsService.GetCategoriesAsync(query, ct));

    public Task<IActionResult> Cashiers([FromQuery] Models.ReportQuery query, CancellationToken ct) =>
        Render(_reportsService.GetCashiersAsync(query, ct));

    public Task<IActionResult> Inventory([FromQuery] Models.ReportQuery query, CancellationToken ct) =>
        Render(_reportsService.GetInventoryAsync(query, ct));

    public Task<IActionResult> Detail(string code, string key, [FromQuery] Models.ReportQuery query,
        CancellationToken ct) => RenderDetail(_reportsService.GetDetailsAsync(code, key, query, ct));

    public async Task<IActionResult> Export(string code, [FromQuery] Models.ReportQuery query,
        CancellationToken ct)
    {
        var action = ActionFor(code);
        if (action is null) return NotFound();
        if (!ReportRangeResolver.TryResolve(query, _clock.GetUtcNow(), out _, out var rangeError))
        {
            TempData["ReportExportError"] = rangeError;
            return RedirectToAction(action, query);
        }

        try
        {
            var report = code.ToUpperInvariant() switch
            {
                "BC-02" => await _reportsService.GetRevenueAsync(query, ct),
                "BC-03" => await _reportsService.GetProductsAsync(query, ct),
                "BC-04" => await _reportsService.GetCategoriesAsync(query, ct),
                "BC-05" => await _reportsService.GetCashiersAsync(query, ct),
                "BC-06" => await _reportsService.GetInventoryAsync(query, ct),
                _ => null
            };
            if (report is null) return NotFound();
            if (report.Message is not null)
            {
                TempData["ReportExportError"] = report.Message;
                return RedirectToAction(action, query);
            }

            var workbook = ReportExcelExporter.Create(report,
                _configuration["Store:Name"] ?? "Cửa hàng tiện lợi",
                User.Identity?.Name ?? "Admin",
                _clock.GetUtcNow());
            var filename = $"{report.Code}_{report.Range.From:yyyy-MM-dd}_{report.Range.To:yyyy-MM-dd}.xlsx";
            return File(workbook,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", filename);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to export {ReportCode}", code);
            TempData["ReportExportError"] = "Không thể tạo file Excel. Vui lòng thử lại.";
            return RedirectToAction(action, query);
        }
    }

    private async Task<IActionResult> Render(Task<Models.OperationalReportResponse> response) =>
        View("Report", await response);

    private async Task<IActionResult> RenderDetail(Task<Models.OperationalReportResponse> response) =>
        View("ReportDetail", await response);

    private static string? ActionFor(string code) => code.ToUpperInvariant() switch
    {
        "BC-02" => "Revenue",
        "BC-03" => "Products",
        "BC-04" => "Categories",
        "BC-05" => "Cashiers",
        "BC-06" => "Inventory",
        _ => null
    };
}
