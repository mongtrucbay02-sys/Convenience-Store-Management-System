using ConvenienceStore.Web.Modules.Reports.Models;
using ConvenienceStore.Web.Modules.Reports.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Modules.Reports.Controllers;

/// <summary>BC-01. Route tạm theo hợp đồng nháp (chờ #4). Chỉ Admin/Quản lý.</summary>
[ApiController]
[Route("api/reports/dashboard")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public class DashboardApiController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardApiController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(DashboardResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardResponse>> Get(CancellationToken cancellationToken) =>
        Ok(await _dashboardService.GetDashboardAsync(cancellationToken));
}
