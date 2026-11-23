
using ConvenienceStore.Web.Models;
using ConvenienceStore.Web.Modules.Auth.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data.Common;
using ConvenienceStore.Web.Shared.Data;

namespace ConvenienceStore.Web.Controllers;

[Authorize(Roles = BangQuyen.Admin)]
public class DashboardController : Controller
{
    private readonly IConfiguration _configuration;

    public DashboardController(IConfiguration configuration)
    {
        _configuration = configuration;
        _ =
            configuration.GetConnectionString("PosMySql")
            ?? configuration.GetConnectionString("CuaHang")
            ?? throw new InvalidOperationException(
                "Chưa cấu hình kết nối MySQL (CuaHang hoặc PosMySql).");
    }

    public async Task<IActionResult> Index()
    {
        var model = new DashboardViewModel();
        var today=DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).Date;

        await using var connection =
            SqlDialect.CreateConnection(_configuration);

        await connection.OpenAsync();

        // 1. Doanh thu và hóa đơn hôm nay
        const string invoiceSql = """
            SELECT
                COALESCE(SUM(tong_thanh_toan), 0)
                    AS doanh_thu,
                COUNT(*) AS so_hoa_don
            FROM hoa_don
            WHERE trang_thai = 'DA_THANH_TOAN'
              AND thanh_toan_luc >= @startDate
              AND thanh_toan_luc < @endDate;
            """;

        await using (var command =
            SqlDialect.Command(invoiceSql, connection))
        {
            command.AddValue(
                "@startDate", today);

            command.AddValue(
                "@endDate", today.AddDays(1));

            await using var reader =
                await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                model.TodayRevenue =
                    Convert.ToInt64(reader["doanh_thu"]);

                model.TodayInvoices =
                    Convert.ToInt32(reader["so_hoa_don"]);
            }
        }

        // 2. Số sản phẩm đang kinh doanh
        const string productSql = """
            SELECT COUNT(*)
            FROM san_pham
            WHERE trang_thai = 'DANG_KINH_DOANH';
            """;

        await using (var command =
            SqlDialect.Command(productSql, connection))
        {
            model.TotalProducts = Convert.ToInt32(
                await command.ExecuteScalarAsync());
        }

        // 3. Cảnh báo tồn kho
        const string stockSql = """
            SELECT COUNT(*)
            FROM san_pham sp
            LEFT JOIN ton_kho tk
                ON tk.san_pham_id = sp.id
            WHERE sp.trang_thai = 'DANG_KINH_DOANH'
              AND (
                  tk.san_pham_id IS NULL
                  OR tk.so_luong_ton <= tk.nguong_toi_thieu
              );
            """;

        await using (var command =
            SqlDialect.Command(stockSql, connection))
        {
            model.LowStockProducts = Convert.ToInt32(
                await command.ExecuteScalarAsync());
        }

        return View(model);
    }
}
