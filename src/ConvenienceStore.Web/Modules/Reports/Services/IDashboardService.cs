using ConvenienceStore.Web.Modules.Reports.Models;

namespace ConvenienceStore.Web.Modules.Reports.Services;

public interface IDashboardService
{
    Task<DashboardResponse> GetDashboardAsync(CancellationToken cancellationToken);
}
