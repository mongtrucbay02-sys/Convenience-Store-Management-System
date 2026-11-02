using ConvenienceStore.Web.Modules.Reports.Models;

namespace ConvenienceStore.Web.Modules.Reports.Services;

public interface IOperationalReportsService
{
    Task<OperationalReportResponse> GetRevenueAsync(ReportQuery query, CancellationToken cancellationToken);
    Task<OperationalReportResponse> GetProductsAsync(ReportQuery query, CancellationToken cancellationToken);
    Task<OperationalReportResponse> GetCategoriesAsync(ReportQuery query, CancellationToken cancellationToken);
    Task<OperationalReportResponse> GetCashiersAsync(ReportQuery query, CancellationToken cancellationToken);
    Task<OperationalReportResponse> GetInventoryAsync(ReportQuery query, CancellationToken cancellationToken);
    Task<OperationalReportResponse> GetPurchasesAsync(ReportQuery query, CancellationToken cancellationToken);
    Task<OperationalReportResponse> GetGrossProfitAsync(ReportQuery query, CancellationToken cancellationToken);
    Task<OperationalReportResponse> GetDetailsAsync(string reportCode, string key, ReportQuery query,
        CancellationToken cancellationToken);
}
