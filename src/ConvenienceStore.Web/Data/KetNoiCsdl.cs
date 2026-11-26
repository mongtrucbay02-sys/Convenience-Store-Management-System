using ConvenienceStore.Web.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Data;

/// <summary>
/// Đọc cấu hình kết nối CSDL và đăng ký <see cref="CuaHangDbContext"/>.
/// Thứ tự ưu tiên của mỗi khóa: biến môi trường → file <c>.env</c> cạnh project → appsettings.
/// Chỉ đọc/ghi dữ liệu; không bao giờ chạy migration hay tạo bảng trên CSDL dùng chung.
/// </summary>
public static class KetNoiCsdl
{
    public const string KhoaChuoiKetNoi = "ConnectionStrings__CuaHang";
    private const string KhoaProvider = "Database__Provider";
    private const string ProviderDuocPhep = "PostgreSql";

    public static WebApplicationBuilder AddCuaHangDatabase(this WebApplicationBuilder builder)
    {
        var thuMucProject = builder.Environment.ContentRootPath;
        var chuoiKetNoi = DocChuoiKetNoi(thuMucProject, builder.Configuration);
        KiemTraProvider(thuMucProject, builder.Configuration);

        builder.Configuration["ConnectionStrings:CuaHang"] = chuoiKetNoi;
        builder.Configuration["Database:Provider"] = ProviderDuocPhep;
        builder.Services.AddDbContext<CuaHangDbContext>(options =>
            options.UseNpgsql(chuoiKetNoi, postgres => postgres.SetPostgresVersion(17, 0)));
        return builder;
    }

    private static string DocChuoiKetNoi(string thuMucProject, IConfiguration configuration)
    {
        var giaTri = DocGiaTri(thuMucProject, configuration, KhoaChuoiKetNoi);
        if (string.IsNullOrWhiteSpace(giaTri))
            throw new InvalidOperationException(
                $"Chưa cấu hình {KhoaChuoiKetNoi}. Sao chép .env.example thành .env cạnh " +
                "ConvenienceStore.Web.csproj và điền chuỗi kết nối, hoặc đặt biến môi trường cùng tên.");

        // Luôn ép search_path và múi giờ của nhóm, kể cả khi .env thiếu hai tham số này.
        return SqlDialect.PostgresConnectionString(giaTri);
    }

    private static void KiemTraProvider(string thuMucProject, IConfiguration configuration)
    {
        var provider = DocGiaTri(thuMucProject, configuration, KhoaProvider) ?? ProviderDuocPhep;
        if (!provider.Equals(ProviderDuocPhep, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Database:Provider chỉ được là PostgreSql.");
    }

    private static string? DocGiaTri(string thuMucProject, IConfiguration configuration, string khoa)
    {
        var bienMoiTruong = Environment.GetEnvironmentVariable(khoa);
        if (!string.IsNullOrWhiteSpace(bienMoiTruong))
            return bienMoiTruong;

        return DocTuFileEnv(Path.Combine(thuMucProject, ".env"), khoa)
            ?? configuration[khoa.Replace("__", ":", StringComparison.Ordinal)];
    }

    private static string? DocTuFileEnv(string duongDan, string khoa)
    {
        if (!File.Exists(duongDan))
            return null;

        foreach (var dong in File.ReadLines(duongDan))
        {
            var noiDung = dong.Trim();
            if (noiDung.Length == 0 || noiDung.StartsWith('#'))
                continue;

            var viTriBang = noiDung.IndexOf('=');
            if (viTriBang < 0 || noiDung[..viTriBang].Trim() != khoa)
                continue;

            var giaTri = BoNgoacKep(noiDung[(viTriBang + 1)..].Trim());
            if (!string.IsNullOrWhiteSpace(giaTri))
                return giaTri;
        }
        return null;
    }

    private static string BoNgoacKep(string giaTri) =>
        giaTri.Length >= 2 && giaTri[0] is ('"' or '\'') && giaTri[^1] == giaTri[0] ? giaTri[1..^1] : giaTri;
}
