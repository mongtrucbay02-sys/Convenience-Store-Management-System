using ConvenienceStore.Web.Modules.Reports.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Modules.Reports.Controllers;

[Area("Reports")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
[ConvenienceStore.Web.Shared.Security.RequirePasswordChanged]
public class ReportsController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly IOperationalReportsService _reportsService;
    private readonly TimeProvider _clock;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ReportsController> _logger;
    private readonly ReportFilterService _filters;
    private readonly WeekEightReportsService _week8;

    public ReportsController(IDashboardService dashboardService, IOperationalReportsService reportsService,
        TimeProvider clock, IConfiguration configuration, ILogger<ReportsController> logger,
        ReportFilterService filters, WeekEightReportsService week8)
    {
        _dashboardService = dashboardService;
        _reportsService = reportsService;
        _clock = clock;
        _configuration = configuration;
        _logger = logger;
        _filters = filters;
        _week8 = week8;
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

    public Task<IActionResult> Purchases([FromQuery] Models.ReportQuery query, CancellationToken ct) =>
        Render(_reportsService.GetPurchasesAsync(query, ct));

    public Task<IActionResult> GrossProfit([FromQuery] Models.ReportQuery query, CancellationToken ct) =>
        Render(_reportsService.GetGrossProfitAsync(query, ct));

    public Task<IActionResult> Detail(string code, string key, [FromQuery] Models.ReportQuery query,
        CancellationToken ct) => RenderDetail(code.ToUpperInvariant() is "BC-09" or "BC-10" or "BC-11"
            ? _week8.GetAsync(code,query,ct,key) : _reportsService.GetDetailsAsync(code, key, query, ct));

    public Task<IActionResult> Returns([FromQuery] Models.ReportQuery query,CancellationToken ct)=>Render(_week8.GetAsync("BC-09",query,ct));
    public Task<IActionResult> Customers([FromQuery] Models.ReportQuery query,CancellationToken ct)=>Render(_week8.GetAsync("BC-10",query,ct));
    public Task<IActionResult> Promotions([FromQuery] Models.ReportQuery query,CancellationToken ct)=>Render(_week8.GetAsync("BC-11",query,ct));

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
                "BC-07" => await _reportsService.GetPurchasesAsync(query, ct),
                "BC-08" => await _reportsService.GetGrossProfitAsync(query, ct),
                "BC-09" or "BC-10" or "BC-11" => await _week8.GetAsync(code,query,ct),
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

    private async Task<IActionResult> Render(Task<Models.OperationalReportResponse> response)
    {
        var report = await response;
        if (report.Code is "BC-03" or "BC-06" or "BC-08")
            ViewData["ReportCategories"] = await _filters.GetCategoriesAsync(
                report.Query.CategoryId, HttpContext.RequestAborted);
        if (report.Code == "BC-07")
            ViewData["ReportSuppliers"] = await _filters.GetSuppliersAsync(
                report.Query.SupplierId, HttpContext.RequestAborted);
        if(report.Code=="BC-09") ViewData["ReportCashiers"]=await _filters.GetCashiersAsync(report.Query.CashierId,HttpContext.RequestAborted);
        if(report.Code=="BC-11") ViewData["ReportPromotions"]=await _filters.GetPromotionsAsync(report.Query.PromotionId,HttpContext.RequestAborted);
        return View("Report", report);
    }

    private async Task<IActionResult> RenderDetail(Task<Models.OperationalReportResponse> response) =>
        View("ReportDetail", await response);

    private static string? ActionFor(string code) => code.ToUpperInvariant() switch
    {
        "BC-02" => "Revenue",
        "BC-03" => "Products",
        "BC-04" => "Categories",
        "BC-05" => "Cashiers",
        "BC-06" => "Inventory",
        "BC-07" => "Purchases",
        "BC-08" => "GrossProfit",
        "BC-09" => "Returns",
        "BC-10" => "Customers",
        "BC-11" => "Promotions",
        _ => null
    };
}
