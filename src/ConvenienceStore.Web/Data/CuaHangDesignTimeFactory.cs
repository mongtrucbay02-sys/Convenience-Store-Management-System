using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace ConvenienceStore.Web.Data;
public sealed class CuaHangDesignTimeFactory : IDesignTimeDbContextFactory<CuaHangDbContext>
{
    public CuaHangDbContext CreateDbContext(string[] args) => new(
        new DbContextOptionsBuilder<CuaHangDbContext>()
            .UseNpgsql("Host=127.0.0.1;Database=smartmart_design_only;Username=unused",
                pg=>pg.SetPostgresVersion(17,0)).Options);
}
