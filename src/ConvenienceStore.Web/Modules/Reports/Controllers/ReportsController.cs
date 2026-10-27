using ConvenienceStore.Web.Modules.Reports.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Modules.Reports.Controllers;

[Area("Reports")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public class ReportsController : Controller
{
    private readonly IDashboardService _dashboardService;

    public ReportsController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    // BC-01: /Reports/Reports/Dashboard
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken) =>
        View(await _dashboardService.GetDashboardAsync(cancellationToken));
}
