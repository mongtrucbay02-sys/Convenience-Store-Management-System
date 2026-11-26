using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace ConvenienceStore.Web.Data;

/// <summary>
/// Chỉ dùng cho lệnh <c>dotnet ef</c>. Cố ý trỏ vào localhost và KHÔNG đọc .env,
/// để lệnh EF không bao giờ chạm vào Supabase dùng chung (CONTRIBUTING.md mục 4.1).
/// </summary>
public sealed class CuaHangDesignTimeFactory : IDesignTimeDbContextFactory<CuaHangDbContext>
{
    public CuaHangDbContext CreateDbContext(string[] args) => new(
        new DbContextOptionsBuilder<CuaHangDbContext>()
            .UseNpgsql("Host=127.0.0.1;Database=smartmart_design_only;Username=unused",
                pg=>pg.SetPostgresVersion(17,0)).Options);
}
