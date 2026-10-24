using ConvenienceStore.Web.Modules.Identity.Entities;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<VaiTro> VaiTros => Set<VaiTro>();

    public DbSet<TaiKhoan> TaiKhoans => Set<TaiKhoan>();

    public DbSet<PhienDangNhap> PhienDangNhaps => Set<PhienDangNhap>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly);
    }
}
