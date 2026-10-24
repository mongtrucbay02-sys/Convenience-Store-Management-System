using ConvenienceStore.Web.Modules.Identity.Entities;
using ConvenienceStore.Web.Modules.Products.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace ConvenienceStore.Web.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<VaiTro> VaiTros => Set<VaiTro>();

    public DbSet<DanhMuc> DanhMucs => Set<DanhMuc>();

    public DbSet<SanPham> SanPhams => Set<SanPham>();

    public DbSet<TaiKhoan> TaiKhoans => Set<TaiKhoan>();

    public DbSet<PhienDangNhap> PhienDangNhaps =>
        Set<PhienDangNhap>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        UpdateTaiKhoanVersions();

        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        UpdateTaiKhoanVersions();

        return base.SaveChangesAsync(
            acceptAllChangesOnSuccess,
            cancellationToken);
    }

    private void UpdateTaiKhoanVersions()
    {
        ChangeTracker.DetectChanges();

        foreach (EntityEntry<TaiKhoan> entry in
                 ChangeTracker.Entries<TaiKhoan>())
        {
            if (entry.State != EntityState.Modified)
                continue;

            var property = entry.Property(x => x.PhienBan);
            var originalVersion = property.OriginalValue;

            // Preserve the original value for the concurrency WHERE clause.
            // Increment only if another field has actually changed.
            bool hasBusinessChanges = entry.Properties.Any(p =>
                p.IsModified &&
                p.Metadata.Name != nameof(TaiKhoan.PhienBan));

            if (!hasBusinessChanges)
                continue;

            property.CurrentValue = checked(originalVersion + 1);
            property.IsModified = true;
        }
    }
}
