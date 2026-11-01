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

    public OperationalReportsApiController(IOperationalReportsService reports, TimeProvider clock)
    {
        _reports = reports;
        _clock = clock;
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

    private async Task<ActionResult<OperationalReportResponse>> Execute(
        ReportQuery query, Func<ReportQuery, Task<OperationalReportResponse>> execute)
    {
        if (!ReportRangeResolver.TryResolve(query, _clock.GetUtcNow(), out _, out var error))
            return BadRequest(new ProblemDetails { Title = "Khoảng thời gian không hợp lệ", Detail = error, Status = 400 });
        return Ok(await execute(query));
    }
}
