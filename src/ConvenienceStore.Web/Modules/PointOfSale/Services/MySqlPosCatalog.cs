
using ConvenienceStore.Web.Models.Pos;
using System.Data.Common;
using ConvenienceStore.Web.Shared.Data;

namespace ConvenienceStore.Web.Services.Pos;

public sealed class MySqlPosCatalog
{
    private readonly IConfiguration _configuration;

    public MySqlPosCatalog(IConfiguration config)
    {
        _configuration = config;
        _ =
            // Ưu tiên PosMySql nếu có; không thì dùng chung chuỗi kết nối
            // CuaHang của Program.cs để chỉ cần cấu hình MỘT chuỗi.
            config.GetConnectionString("PosMySql")
            ?? config.GetConnectionString("CuaHang")
            ?? throw new InvalidOperationException(
                "Chưa cấu hình ConnectionStrings:CuaHang (hoặc PosMySql)");
    }

    private bool IsPostgres => string.Equals(_configuration["Database:Provider"],"PostgreSql",StringComparison.OrdinalIgnoreCase);
    public DbConnection CreateConnection()
        => SqlDialect.CreateConnection(_configuration);

    // The shared current-price view uses NOW(). Match CuaHangDbContext's +07:00
    // session so a just-effective price is visible to POS at the same instant.
    private async Task<DbConnection> OpenPriceConnectionAsync()
    {
        var connection = CreateConnection();
        try {
            await connection.OpenAsync();
            await using var command = SqlDialect.Command(connection.Sql(ConvenienceStore.Web.Data.GioVietNamInterceptor.CauLenhDatMuiGio, "SET TIME ZONE 'Asia/Ho_Chi_Minh'"), connection);
            await command.ExecuteNonQueryAsync(); return connection;
        }
        catch { await connection.DisposeAsync(); throw; }
    }

    public async Task<List<PosProduct>> SearchAsync(
        string? keyword, int? categoryId = null)
    {
        var sql = $"""
            SELECT
                sp.id,
                sp.ma,
                sp.ten_san_pham,
                sp.anh_url,
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
                  OR {VietnameseSearch.Column(IsPostgres,"sp.ma")} LIKE @pattern
                  OR {VietnameseSearch.Column(IsPostgres,"sp.ten_san_pham")} LIKE @pattern
              )
              AND (
                  @categoryId IS NULL
                  OR sp.danh_muc_id = @categoryId
              )
            ORDER BY sp.ten_san_pham
            LIMIT 100;
            """;

        var products = new List<PosProduct>();

        await using var connection = await OpenPriceConnectionAsync();

        await using var command =
            SqlDialect.Command(sql, connection);

        var search = keyword?.Trim() ?? "";
        if(IsPostgres) search=VietnameseSearch.Fold(search);

        command.AddValue("@keyword", search);
        command.AddValue("@pattern", $"%{search}%");
        command.AddValue(
            "@categoryId", (object?)categoryId ?? DBNull.Value);

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            products.Add(new PosProduct
            {
                Id = Convert.ToInt32(reader["id"]),
                Code = Convert.ToString(reader["ma"]) ?? "",
                Name = Convert.ToString(reader["ten_san_pham"]) ?? "",
                ImageUrl = reader["anh_url"] as string,
                Price = Convert.ToInt64(reader["gia_ban"]),
                Stock = Convert.ToInt32(reader["so_luong_ton"])
            });
        }

        return products;
    }

    public async Task<List<PosCategory>> GetCategoriesAsync()
    {
        var sql = $"""
            SELECT id, ten FROM danh_muc
            WHERE trang_thai = 'HOAT_DONG'
            ORDER BY ten;
            """;
        var categories = new List<PosCategory>();
        await using var connection = await OpenPriceConnectionAsync();
        await using var command = SqlDialect.Command(sql, connection);
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
        var sql = $"""
            SELECT sp.id, sp.ma, sp.ten_san_pham, sp.anh_url,
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
        await using var connection = await OpenPriceConnectionAsync();
        await using var command = SqlDialect.Command(sql, connection);
        command.AddValue("@productId", productId);
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        return new PosProduct
        {
            Id = Convert.ToInt32(reader["id"]),
            Code = Convert.ToString(reader["ma"]) ?? "",
            Name = Convert.ToString(reader["ten_san_pham"]) ?? "",
            ImageUrl = reader["anh_url"] as string,
            Price = Convert.ToInt64(reader["gia_ban"]),
            Stock = Convert.ToInt32(reader["so_luong_ton"])
        };
    }
}
