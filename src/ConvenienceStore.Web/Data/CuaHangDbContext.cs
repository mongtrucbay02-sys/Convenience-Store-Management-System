using System;
using System.Collections.Generic;
using ConvenienceStore.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Data;

public partial class CuaHangDbContext : DbContext
{
    public CuaHangDbContext(DbContextOptions<CuaHangDbContext> options)
        : base(options)
    {
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        TangPhienBanThucTheBiSua();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        TangPhienBanThucTheBiSua();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void TangPhienBanThucTheBiSua()
    {
        foreach (var entry in ChangeTracker.Entries().Where(e=>e.Entity is DanhMuc or ChuongTrinhKhuyenMai))
        {
            if (entry.State == EntityState.Modified)
            {
                var phienBanGoc = entry.OriginalValues.GetValue<long>(
                    nameof(DanhMuc.PhienBan));
                // Adapt #168's business-property guard while keeping #204/#207's
                // retry semantics. Detached Update has equal local originals, so
                // business IsModified flags must remain authoritative.
                var coThayDoi = entry.Properties.Any(property =>
                    property.IsModified &&
                    property.Metadata.Name != nameof(DanhMuc.PhienBan));
                var phienBan = entry.Property(nameof(DanhMuc.PhienBan));
                phienBan.CurrentValue = coThayDoi ? checked(phienBanGoc + 1) : phienBanGoc;
                phienBan.IsModified = coThayDoi;
            }
        }
    }

    public virtual DbSet<BoDemChungTu> BoDemChungTus { get; set; }

    public virtual DbSet<ChinhSachDoiTra> ChinhSachDoiTras { get; set; }

    public virtual DbSet<ChuongTrinhKhuyenMai> ChuongTrinhKhuyenMais { get; set; }

    public virtual DbSet<CtGioHang> CtGioHangs { get; set; }

    public virtual DbSet<CtHoaDon> CtHoaDons { get; set; }

    public virtual DbSet<CtKiemKe> CtKiemKes { get; set; }

    public virtual DbSet<CtPhieuDoiTra> CtPhieuDoiTras { get; set; }

    public virtual DbSet<CtPhieuNhap> CtPhieuNhaps { get; set; }

    public virtual DbSet<CtXuatHuy> CtXuatHuys { get; set; }

    public virtual DbSet<DanhMuc> DanhMucs { get; set; }

    public virtual DbSet<GioHang> GioHangs { get; set; }

    public virtual DbSet<HoaDon> HoaDons { get; set; }

    public virtual DbSet<KhachHang> KhachHangs { get; set; }

    public virtual DbSet<LichSuDiem> LichSuDiems { get; set; }

    public virtual DbSet<LichSuGium> LichSuGia { get; set; }

    public virtual DbSet<NhaCungCap> NhaCungCaps { get; set; }

    public virtual DbSet<PhienDangNhap> PhienDangNhaps { get; set; }

    public virtual DbSet<PhieuDoiTra> PhieuDoiTras { get; set; }

    public virtual DbSet<PhieuKiemKe> PhieuKiemKes { get; set; }

    public virtual DbSet<PhieuNhap> PhieuNhaps { get; set; }

    public virtual DbSet<PhieuXuatHuy> PhieuXuatHuys { get; set; }

    public virtual DbSet<QuyTacTichDiem> QuyTacTichDiems { get; set; }

    public virtual DbSet<SanPham> SanPhams { get; set; }

    public virtual DbSet<TaiKhoan> TaiKhoans { get; set; }

    public virtual DbSet<TheKho> TheKhos { get; set; }

    public virtual DbSet<TonKho> TonKhos { get; set; }

    public virtual DbSet<VChuongTrinhKhuyenMai> VChuongTrinhKhuyenMais { get; set; }

    public virtual DbSet<VGiaBanHienHanh> VGiaBanHienHanhs { get; set; }

    public virtual DbSet<VLuotDungKhuyenMai> VLuotDungKhuyenMais { get; set; }

    public virtual DbSet<VaiTro> VaiTros { get; set; }

    public virtual DbSet<GiaoDichTienMat> GiaoDichTienMats { get; set; }
    public virtual DbSet<HoanTienHoaDon> HoanTienHoaDons { get; set; }
    public virtual DbSet<QuyTienMat> QuyTienMats { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => ConfigurePostgreSqlModel(modelBuilder);
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(pg=>pg.MigrationsHistoryTable("__EFMigrationsHistory","public"));
        optionsBuilder.AddInterceptors(new PostgreSqlTimeInterceptor());
        base.OnConfiguring(optionsBuilder);
    }
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
