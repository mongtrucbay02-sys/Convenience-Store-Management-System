
namespace ConvenienceStore.Web.Models;

public class DashboardViewModel
{
    public long TodayRevenue { get; set; }

    public int TodayInvoices { get; set; }

    public int TotalProducts { get; set; }

    public int LowStockProducts { get; set; }

    public DateTime ReportDate { get; set; } = DateTime.Today;
}
