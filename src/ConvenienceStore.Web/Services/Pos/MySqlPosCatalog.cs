
using ConvenienceStore.Web.Models.Pos;
using MySqlConnector;

namespace ConvenienceStore.Web.Services.Pos;

public sealed class MySqlPosCatalog
{
    private readonly string _connectionString;

    public MySqlPosCatalog(IConfiguration config)
    {
        _connectionString =
            // Ưu tiên PosMySql nếu có; không thì dùng chung chuỗi kết nối
            // CuaHang của Program.cs để chỉ cần cấu hình MỘT chuỗi.
            config.GetConnectionString("PosMySql")
            ?? config.GetConnectionString("CuaHang")
            ?? throw new InvalidOperationException(
                "Chưa cấu hình ConnectionStrings:CuaHang (hoặc PosMySql)");
    }

    public MySqlConnection CreateConnection()
        => new MySqlConnection(_connectionString);

    public async Task<List<PosProduct>> SearchAsync(
        string? keyword, int? categoryId = null)
    {
        const string sql = """
            SELECT
                sp.id,
                sp.ma,
                sp.ten_san_pham,
                g.gia_ban,
                tk.so_luong_ton
            FROM san_pham sp
            INNER JOIN danh_muc dm
                ON dm.id = sp.danh_muc_id
            INNER JOIN v_gia_ban_hien_hanh g
                ON g.san_pham_id = sp.id
            INNER JOIN ton_kho tk
                ON tk.san_pham_id = sp.id
            WHERE sp.trang_thai = 'DANG_KINH_DOANH'
              AND dm.trang_thai = 'HOAT_DONG'
              AND (
                  @keyword = ''
                  OR sp.ma LIKE @pattern
                  OR sp.ten_san_pham LIKE @pattern
              )
              AND (
                  @categoryId IS NULL
                  OR sp.danh_muc_id = @categoryId
              )
            ORDER BY sp.ten_san_pham
            LIMIT 100;
            """;

        var products = new List<PosProduct>();

        await using var connection = CreateConnection();
        await connection.OpenAsync();

        await using var command =
            new MySqlCommand(sql, connection);

        var search = keyword?.Trim() ?? "";

        command.Parameters.AddWithValue("@keyword", search);
        command.Parameters.AddWithValue("@pattern", $"%{search}%");
        command.Parameters.AddWithValue(
            "@categoryId", (object?)categoryId ?? DBNull.Value);

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            products.Add(new PosProduct
            {
                Id = Convert.ToInt32(reader["id"]),
                Code = Convert.ToString(reader["ma"]) ?? "",
                Name = Convert.ToString(reader["ten_san_pham"]) ?? "",
                Price = Convert.ToInt64(reader["gia_ban"]),
                Stock = Convert.ToInt32(reader["so_luong_ton"])
            });
        }

        return products;
    }

    public async Task<List<PosCategory>> GetCategoriesAsync()
    {
        const string sql = """
            SELECT id, ten FROM danh_muc
            WHERE trang_thai = 'HOAT_DONG'
            ORDER BY ten;
            """;
        var categories = new List<PosCategory>();
        await using var connection = CreateConnection();
        await connection.OpenAsync();
        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            categories.Add(new PosCategory
            {
                Id = Convert.ToInt32(reader["id"]),
                Name = Convert.ToString(reader["ten"]) ?? ""
            });
        }
        return categories;
    }

    public async Task<PosProduct?> FindAsync(int productId)
    {
        if (productId <= 0) return null;
        const string sql = """
            SELECT sp.id, sp.ma, sp.ten_san_pham,
                   g.gia_ban, tk.so_luong_ton
            FROM san_pham sp
            INNER JOIN danh_muc dm ON dm.id = sp.danh_muc_id
            INNER JOIN v_gia_ban_hien_hanh g ON g.san_pham_id = sp.id
            INNER JOIN ton_kho tk ON tk.san_pham_id = sp.id
            WHERE sp.id = @productId
              AND sp.trang_thai = 'DANG_KINH_DOANH'
              AND dm.trang_thai = 'HOAT_DONG'
            LIMIT 1;
            """;
        await using var connection = CreateConnection();
        await connection.OpenAsync();
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@productId", productId);
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        return new PosProduct
        {
            Id = Convert.ToInt32(reader["id"]),
            Code = Convert.ToString(reader["ma"]) ?? "",
            Name = Convert.ToString(reader["ten_san_pham"]) ?? "",
            Price = Convert.ToInt64(reader["gia_ban"]),
            Stock = Convert.ToInt32(reader["so_luong_ton"])
        };
    }
}
