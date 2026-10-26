using ConvenienceStore.Web.Modules.Customers.Entities;
using ConvenienceStore.Web.Modules.PointOfSale.Entities;
using ConvenienceStore.Web.Modules.Promotions.Entities;
using ConvenienceStore.Web.Modules.Inventory.Entities;
using ConvenienceStore.Web.Modules.Shared.Entities;
using ConvenienceStore.Web.Modules.Purchasing.Entities;
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

    public DbSet<TonKho> TonKhos => Set<TonKho>();
    public DbSet<TheKho> TheKhos => Set<TheKho>();
    public DbSet<PhieuKiemKe> PhieuKiemKes => Set<PhieuKiemKe>();
    public DbSet<CtKiemKe> CtKiemKes => Set<CtKiemKe>();
    public DbSet<PhieuXuatHuy> PhieuXuatHuys => Set<PhieuXuatHuy>();
    public DbSet<CtXuatHuy> CtXuatHuys => Set<CtXuatHuy>();
    public DbSet<KhachHang> KhachHangs => Set<KhachHang>();
    public DbSet<GioHang> GioHangs => Set<GioHang>();
    public DbSet<CtGioHang> CtGioHangs => Set<CtGioHang>();
    public DbSet<HoaDon> HoaDons => Set<HoaDon>();
    public DbSet<QuyTacTichDiem> QuyTacTichDiems => Set<QuyTacTichDiem>();
    public DbSet<ChuongTrinhKhuyenMai> ChuongTrinhKhuyenMais => Set<ChuongTrinhKhuyenMai>();
    public DbSet<KhuyenMaiSanPham> KhuyenMaiSanPhams => Set<KhuyenMaiSanPham>();

    public DbSet<BoDemChungTu> BoDemChungTus =>
        Set<BoDemChungTu>();

    public DbSet<VaiTro> VaiTros => Set<VaiTro>();

    public DbSet<DanhMuc> DanhMucs => Set<DanhMuc>();

    public DbSet<SanPham> SanPhams => Set<SanPham>();

    public DbSet<LichSuGia> LichSuGias => Set<LichSuGia>();

    public DbSet<NhaCungCap> NhaCungCaps => Set<NhaCungCap>();

    public DbSet<PhieuNhap> PhieuNhaps => Set<PhieuNhap>();

    public DbSet<ChiTietPhieuNhap> ChiTietPhieuNhaps => Set<ChiTietPhieuNhap>();

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
        UpdateConcurrencyVersions();

        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        UpdateConcurrencyVersions();

        return base.SaveChangesAsync(
            acceptAllChangesOnSuccess,
            cancellationToken);
    }

    private void UpdateConcurrencyVersions()
    {
        ChangeTracker.DetectChanges();

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State != EntityState.Modified)
                continue;

            var versionProperty = entry.Metadata.FindProperty("PhienBan");

            if (versionProperty is null || !versionProperty.IsConcurrencyToken)
                continue;

            if (versionProperty.ClrType != typeof(uint))
                throw new InvalidOperationException(
                    "Unsupported concurrency version type.");

            var property = entry.Property("PhienBan");

            bool hasBusinessChanges = entry.Properties.Any(p =>
                p.IsModified &&
                p.Metadata.Name != "PhienBan");

            if (!hasBusinessChanges)
                continue;

            var originalVersion = (uint)property.OriginalValue!;

            property.CurrentValue = checked(originalVersion + 1);
            property.IsModified = true;
        }
    }
}