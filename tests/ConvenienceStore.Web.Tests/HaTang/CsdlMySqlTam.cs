using ConvenienceStore.Web.Data;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using Xunit;

namespace ConvenienceStore.Web.Tests.HaTang;

/// <summary>
/// T4-17: một CSDL MySQL thật, riêng cho mỗi lần chạy test (Tài liệu dịch vụ kho 1.2 mục 6).
/// Tạo CSDL cuahang_test_xxxxxxxx, dựng bằng migration (InitialCreate = SchemaV1, đủ CHECK
/// và trigger như CSDL thật), chạy test xong thì xóa.
/// </summary>
/// <remarks>
/// Chuỗi kết nối đọc từ biến môi trường <c>ConnectionStrings__CuaHangTest</c>, dùng tài khoản
/// có quyền CREATE/DROP DATABASE (root của MySQL trong Docker Compose hoặc service container của CI).
/// Phần database trong chuỗi kết nối bị bỏ qua. Đặt <c>GIU_CSDL_TEST=1</c> để giữ CSDL lại khi cần xem lỗi.
/// </remarks>
public class CsdlMySqlTam : IAsyncLifetime
{
    public const string TenBienMoiTruong = "ConnectionStrings__CuaHangTest";

    private string _chuoiMayChu = "";

    /// <summary>Tên CSDL tạm của lần chạy này.</summary>
    public string TenCsdl { get; } = "cuahang_test_" + Guid.NewGuid().ToString("N")[..8];

    /// <summary>Chuỗi kết nối tới CSDL tạm.</summary>
    public string ChuoiKetNoi { get; private set; } = "";

    public DbContextOptions<CuaHangDbContext> TuyChonDbContext { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var chuoi = Environment.GetEnvironmentVariable(TenBienMoiTruong);
        if (string.IsNullOrWhiteSpace(chuoi))
        {
            // Báo lỗi rõ ràng thay vì bỏ qua test, để CI không báo đạt giả.
            throw new InvalidOperationException(
                $"Thiếu biến môi trường {TenBienMoiTruong}. Ví dụ (Git Bash): " +
                $"export {TenBienMoiTruong}=\"server=localhost;port=3306;user=root;password=...\"");
        }

        var mayChu = new MySqlConnectionStringBuilder(chuoi) { Database = "" };
        _chuoiMayChu = mayChu.ConnectionString;
        ChuoiKetNoi = new MySqlConnectionStringBuilder(chuoi) { Database = TenCsdl }.ConnectionString;

        await ChayTrenMayChuAsync(
            $"CREATE DATABASE `{TenCsdl}` CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci");

        TuyChonDbContext = new DbContextOptionsBuilder<CuaHangDbContext>()
            .UseMySQL(ChuoiKetNoi)
            .AddInterceptors(new GioVietNamInterceptor())
            .Options;

        await using (var db = TaoDbContext())
        {
            await db.Database.MigrateAsync();
        }

        await NapDuLieuNenAsync();
    }

    public async Task DisposeAsync()
    {
        MySqlConnection.ClearAllPools();
        if (Environment.GetEnvironmentVariable("GIU_CSDL_TEST") == "1" || _chuoiMayChu.Length == 0)
            return;
        await ChayTrenMayChuAsync($"DROP DATABASE IF EXISTS `{TenCsdl}`");
    }

    /// <summary>Lớp con thêm dữ liệu nền dùng chung (gọi một lần sau migration).</summary>
    protected virtual Task NapDuLieuNenAsync() => Task.CompletedTask;

    /// <summary>DbContext mới, kết nối mới; dùng để gọi dịch vụ hoặc đọc lại kết quả đã COMMIT.</summary>
    public CuaHangDbContext TaoDbContext() => new(TuyChonDbContext);

    /// <summary>Kết nối ADO.NET mới tới CSDL tạm, đã mở và đặt múi giờ +07:00 như ứng dụng.</summary>
    public async Task<MySqlConnection> MoKetNoiAsync()
    {
        var ketNoi = new MySqlConnection(ChuoiKetNoi);
        await ketNoi.OpenAsync();
        await using var lenh = new MySqlCommand(GioVietNamInterceptor.CauLenhDatMuiGio, ketNoi);
        await lenh.ExecuteNonQueryAsync();
        return ketNoi;
    }

    /// <summary>Chạy một câu lệnh không trả dữ liệu trên kết nối mới.</summary>
    public async Task<int> ChayAsync(string sql, params (string Ten, object? GiaTri)[] thamSo)
    {
        await using var ketNoi = await MoKetNoiAsync();
        await using var lenh = TaoLenh(ketNoi, sql, thamSo);
        return await lenh.ExecuteNonQueryAsync();
    }

    /// <summary>Đọc một giá trị (dòng đầu, cột đầu) trên kết nối mới; null nếu không có.</summary>
    public async Task<object?> DocMotGiaTriAsync(string sql, params (string Ten, object? GiaTri)[] thamSo)
    {
        await using var ketNoi = await MoKetNoiAsync();
        await using var lenh = TaoLenh(ketNoi, sql, thamSo);
        var kq = await lenh.ExecuteScalarAsync();
        return kq is DBNull ? null : kq;
    }

    public static MySqlCommand TaoLenh(
        MySqlConnection ketNoi, string sql, params (string Ten, object? GiaTri)[] thamSo)
    {
        var lenh = new MySqlCommand(sql, ketNoi);
        foreach (var (ten, giaTri) in thamSo)
            lenh.Parameters.AddWithValue(ten, giaTri ?? DBNull.Value);
        return lenh;
    }

    private async Task ChayTrenMayChuAsync(string sql)
    {
        await using var ketNoi = new MySqlConnection(_chuoiMayChu);
        await ketNoi.OpenAsync();
        await using var lenh = new MySqlCommand(sql, ketNoi);
        await lenh.ExecuteNonQueryAsync();
    }
}
