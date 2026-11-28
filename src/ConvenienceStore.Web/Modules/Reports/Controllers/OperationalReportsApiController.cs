using ConvenienceStore.Web.Modules.Reports.Models;
using ConvenienceStore.Web.Modules.Reports.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Modules.Reports.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class OperationalReportsApiController : ControllerBase
{
    private readonly IOperationalReportsService _reports;
    private readonly TimeProvider _clock;
    private readonly WeekEightReportsService _week8;

    public OperationalReportsApiController(IOperationalReportsService reports, TimeProvider clock, WeekEightReportsService week8)
    {
        _reports = reports;
        _clock = clock;
        _week8 = week8;
    }

    [HttpGet("revenue")]
    public Task<ActionResult<OperationalReportResponse>> Revenue([FromQuery] ReportQuery query, CancellationToken ct) =>
        Execute(query, q => _reports.GetRevenueAsync(q, ct));

    [HttpGet("products")]
    public Task<ActionResult<OperationalReportResponse>> Products([FromQuery] ReportQuery query, CancellationToken ct) =>
        Execute(query, q => _reports.GetProductsAsync(q, ct));

    [HttpGet("categories")]
    public Task<ActionResult<OperationalReportResponse>> Categories([FromQuery] ReportQuery query, CancellationToken ct) =>
        Execute(query, q => _reports.GetCategoriesAsync(q, ct));

    [HttpGet("cashiers")]
    public Task<ActionResult<OperationalReportResponse>> Cashiers([FromQuery] ReportQuery query, CancellationToken ct) =>
        Execute(query, q => _reports.GetCashiersAsync(q, ct));

    [HttpGet("inventory")]
    public Task<ActionResult<OperationalReportResponse>> Inventory([FromQuery] ReportQuery query, CancellationToken ct) =>
        Execute(query, q => _reports.GetInventoryAsync(q, ct));

    [HttpGet("purchases")]
    public Task<ActionResult<OperationalReportResponse>> Purchases([FromQuery] ReportQuery query, CancellationToken ct) =>
        Execute(query, q => _reports.GetPurchasesAsync(q, ct));

    [HttpGet("gross-profit")]
    public Task<ActionResult<OperationalReportResponse>> GrossProfit([FromQuery] ReportQuery query, CancellationToken ct) =>
        Execute(query, q => _reports.GetGrossProfitAsync(q, ct));

    [HttpGet("returns")]
    public Task<ActionResult<OperationalReportResponse>> Returns([FromQuery] ReportQuery query,CancellationToken ct)=>Execute(query,q=>_week8.GetAsync("BC-09",q,ct));
    [HttpGet("customers")]
    public Task<ActionResult<OperationalReportResponse>> Customers([FromQuery] ReportQuery query,CancellationToken ct)=>Execute(query,q=>_week8.GetAsync("BC-10",q,ct));
    [HttpGet("promotions")]
    public Task<ActionResult<OperationalReportResponse>> Promotions([FromQuery] ReportQuery query,CancellationToken ct)=>Execute(query,q=>_week8.GetAsync("BC-11",q,ct));

    private async Task<ActionResult<OperationalReportResponse>> Execute(
        ReportQuery query, Func<ReportQuery, Task<OperationalReportResponse>> execute)
    {
        if (!ReportRangeResolver.TryResolve(query, _clock.GetUtcNow(), out _, out var error))
            return BadRequest(new ProblemDetails { Title = "Khoảng thời gian không hợp lệ", Detail = error, Status = 400 });
        if(query.CashierId is <=0 || query.PromotionId is <=0 || (!string.IsNullOrWhiteSpace(query.ReturnReason) && !WeekEightReportsService.Reasons.ContainsKey(query.ReturnReason)))
            return BadRequest(new ProblemDetails {Title="Bộ lọc không hợp lệ",Status=400});
        return Ok(await execute(query));
    }
}
