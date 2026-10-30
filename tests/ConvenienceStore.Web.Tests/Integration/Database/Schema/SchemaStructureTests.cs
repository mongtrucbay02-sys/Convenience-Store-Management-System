using System.Data;
using MySql.Data.MySqlClient;
using Xunit;

namespace ConvenienceStore.Web.Tests.Integration.Database.Schema;

/// <summary>
/// Opt-in integration tests. Run only against a known read-only-accessible MySQL schema.
/// Set RUN_MYSQL_INTEGRATION_TESTS=1 and TEST_MYSQL_CONNECTION in the test process.
/// Never embed passwords or run this suite against an unintended database.
/// </summary>
public sealed class SchemaStructureTests
{
    private const string ExpectedDatabase = "cuahang_codefirst_dev";
    private const string ExpectedLatestMigration = "20261027004309_DatabaseViewsAndStockCardTriggers";

    private static readonly string[] BusinessTables =
    [
        "bo_dem_chung_tu", "chinh_sach_doi_tra", "chuong_trinh_khuyen_mai",
        "ct_gio_hang", "ct_hoa_don", "ct_kiem_ke", "ct_phieu_doi_tra",
        "ct_phieu_nhap", "ct_xuat_huy", "danh_muc", "gio_hang", "hoa_don",
        "khach_hang", "khuyen_mai_san_pham", "lich_su_diem", "lich_su_gia",
        "nha_cung_cap", "phien_dang_nhap", "phieu_doi_tra", "phieu_kiem_ke",
        "phieu_nhap", "phieu_xuat_huy", "quy_tac_tich_diem", "san_pham",
        "tai_khoan", "the_kho", "ton_kho", "vai_tro"
    ];

    private static readonly string[] ExpectedViews =
    [
        "v_chuong_trinh_khuyen_mai", "v_gia_ban_hien_hanh", "v_luot_dung_khuyen_mai"
    ];

    private static MySqlConnection OpenConnection()
    {
        string? secret = Environment.GetEnvironmentVariable("TEST_MYSQL_CONNECTION");
        Assert.False(string.IsNullOrWhiteSpace(secret),
            "Set TEST_MYSQL_CONNECTION in the test process (do not paste it into logs).");

        var options = new MySqlConnectionStringBuilder(secret)
        {
            Pooling = false
        };
        Assert.Equal(ExpectedDatabase, options.Database);

        var connection = new MySqlConnection(options.ConnectionString);
        try
        {
            connection.Open();
            Assert.Equal(ExpectedDatabase, connection.Database);
            return connection;
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }

    private static async Task<List<string>> ReadNamesAsync(MySqlConnection db, string sql)
    {
        using var cmd = new MySqlCommand(sql, db) { CommandTimeout = 20 };
        using var reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
        var names = new List<string>();
        while (await reader.ReadAsync()) names.Add(reader.GetString(0));
        return names;
    }

    private static async Task<long> ScalarLongAsync(MySqlConnection db, string sql)
    {
        using var cmd = new MySqlCommand(sql, db) { CommandTimeout = 20 };
        return Convert.ToInt64(await cmd.ExecuteScalarAsync());
    }

    [MySqlIntegrationFact]
    [Trait("Category", "DatabaseIntegration")]
    public async Task DB01_HasExactly29BaseTables()
    {
        using var db = OpenConnection();
        Assert.Equal(29L, await ScalarLongAsync(db, """
            SELECT COUNT(*) FROM information_schema.TABLES
            WHERE TABLE_SCHEMA = DATABASE() AND TABLE_TYPE = 'BASE TABLE'
            """));
    }

    [MySqlIntegrationFact]
    [Trait("Category", "DatabaseIntegration")]
    public async Task DB02_HasAll28ExpectedBusinessTables()
    {
        using var db = OpenConnection();
        var actual = await ReadNamesAsync(db, """
            SELECT TABLE_NAME FROM information_schema.TABLES
            WHERE TABLE_SCHEMA = DATABASE() AND TABLE_TYPE = 'BASE TABLE'
              AND TABLE_NAME <> '__EFMigrationsHistory'
            ORDER BY TABLE_NAME
            """);
        Assert.Equal(BusinessTables.OrderBy(x => x), actual.OrderBy(x => x));
    }

    [MySqlIntegrationFact]
    [Trait("Category", "DatabaseIntegration")]
    public async Task DB03_HasExactlyThreeExpectedViews()
    {
        using var db = OpenConnection();
        var actual = await ReadNamesAsync(db, """
            SELECT TABLE_NAME FROM information_schema.VIEWS
            WHERE TABLE_SCHEMA = DATABASE() ORDER BY TABLE_NAME
            """);
        Assert.Equal(ExpectedViews.OrderBy(x => x), actual.OrderBy(x => x));
    }

    [MySqlIntegrationTheory]
    [InlineData("v_chuong_trinh_khuyen_mai")]
    [InlineData("v_gia_ban_hien_hanh")]
    [InlineData("v_luot_dung_khuyen_mai")]
    [Trait("Category", "DatabaseIntegration")]
    public async Task DB04_ViewCanBePreparedAndQueriedWithoutReadingRows(string view)
    {
        using var db = OpenConnection();
        // Literal from the fixed InlineData set; NEVER accept user-supplied identifiers here.
        Assert.Contains(view, ExpectedViews);
        using var command = new MySqlCommand($"SELECT * FROM `{view}` LIMIT 0", db)
            { CommandTimeout = 20 };
        using var reader = (MySqlDataReader)await command.ExecuteReaderAsync();
        Assert.True(reader.FieldCount > 0);
    }

    [MySqlIntegrationFact]
    [Trait("Category", "DatabaseIntegration")]
    public async Task DB05_UpdateProtectionTriggerExists()
    {
        using var db = OpenConnection();
        Assert.Equal(1L, await ScalarLongAsync(db, """
            SELECT COUNT(*) FROM information_schema.TRIGGERS
            WHERE TRIGGER_SCHEMA = DATABASE()
              AND TRIGGER_NAME = 'trg_the_kho_khong_sua'
              AND EVENT_OBJECT_TABLE = 'the_kho'
              AND ACTION_TIMING = 'BEFORE'
              AND EVENT_MANIPULATION = 'UPDATE'
            """));
    }

    [MySqlIntegrationFact]
    [Trait("Category", "DatabaseIntegration")]
    public async Task DB06_DeleteProtectionTriggerExists()
    {
        using var db = OpenConnection();
        Assert.Equal(1L, await ScalarLongAsync(db, """
            SELECT COUNT(*) FROM information_schema.TRIGGERS
            WHERE TRIGGER_SCHEMA = DATABASE()
              AND TRIGGER_NAME = 'trg_the_kho_khong_xoa'
              AND EVENT_OBJECT_TABLE = 'the_kho'
              AND ACTION_TIMING = 'BEFORE'
              AND EVENT_MANIPULATION = 'DELETE'
            """));
    }

    [MySqlIntegrationFact]
    [Trait("Category", "DatabaseIntegration")]
    public async Task DB07_Has23AppliedMigrations()
    {
        using var db = OpenConnection();
        Assert.Equal(23L, await ScalarLongAsync(db,
            "SELECT COUNT(*) FROM __EFMigrationsHistory"));
    }

    [MySqlIntegrationFact]
    [Trait("Category", "DatabaseIntegration")]
    public async Task DB08_LatestMigrationMatchesBaseline()
    {
        using var db = OpenConnection();
        var migrations = await ReadNamesAsync(db,
            "SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC LIMIT 1");
        Assert.Equal(new[] { ExpectedLatestMigration }, migrations);
    }

    [MySqlIntegrationFact]
    [Trait("Category", "DatabaseIntegration")]
    public async Task DB09_StockCardHasPrimaryKey()
    {
        using var db = OpenConnection();
        Assert.True(await ScalarLongAsync(db, """
            SELECT COUNT(*) FROM information_schema.TABLE_CONSTRAINTS
            WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'the_kho'
              AND CONSTRAINT_TYPE = 'PRIMARY KEY'
            """) >= 1);
    }

    [MySqlIntegrationFact]
    [Trait("Category", "DatabaseIntegration")]
    public async Task DB10_ProductHasPrimaryKey()
    {
        using var db = OpenConnection();
        Assert.True(await ScalarLongAsync(db, """
            SELECT COUNT(*) FROM information_schema.TABLE_CONSTRAINTS
            WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'san_pham'
              AND CONSTRAINT_TYPE = 'PRIMARY KEY'
            """) >= 1);
    }
}

public sealed class MySqlIntegrationFactAttribute : FactAttribute
{
    public MySqlIntegrationFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("RUN_MYSQL_INTEGRATION_TESTS") != "1")
            Skip = "Opt-in: set RUN_MYSQL_INTEGRATION_TESTS=1 and TEST_MYSQL_CONNECTION.";
    }
}

public sealed class MySqlIntegrationTheoryAttribute : TheoryAttribute
{
    public MySqlIntegrationTheoryAttribute()
    {
        if (Environment.GetEnvironmentVariable("RUN_MYSQL_INTEGRATION_TESTS") != "1")
            Skip = "Opt-in: set RUN_MYSQL_INTEGRATION_TESTS=1 and TEST_MYSQL_CONNECTION.";
    }
}
