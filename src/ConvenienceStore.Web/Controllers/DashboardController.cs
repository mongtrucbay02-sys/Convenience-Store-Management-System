
using ConvenienceStore.Web.Models;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace ConvenienceStore.Web.Controllers;

public class DashboardController : Controller
{
    private readonly string _connectionString;

    public DashboardController(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("PosMySql")
            ?? throw new InvalidOperationException(
                "Chưa cấu hình kết nối MySQL PosMySql.");
    }

    public async Task<IActionResult> Index()
    {
        var model = new DashboardViewModel();

        await using var connection =
            new MySqlConnection(_connectionString);

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
            new MySqlCommand(invoiceSql, connection))
        {
            command.Parameters.AddWithValue(
                "@startDate", DateTime.Today);

            command.Parameters.AddWithValue(
                "@endDate", DateTime.Today.AddDays(1));

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
            new MySqlCommand(productSql, connection))
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
            new MySqlCommand(stockSql, connection))
        {
            model.LowStockProducts = Convert.ToInt32(
                await command.ExecuteScalarAsync());
        }

        return View(model);
    }
}
