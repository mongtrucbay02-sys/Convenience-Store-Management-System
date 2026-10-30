using ConvenienceStore.Web.Modules.Purchasing.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConvenienceStore.Web.Modules.Purchasing.Configurations;

public sealed class ChiTietPhieuNhapConfiguration
    : IEntityTypeConfiguration<ChiTietPhieuNhap>
{
    public void Configure(EntityTypeBuilder<ChiTietPhieuNhap> builder)
    {
        builder.ToTable("ct_phieu_nhap", table =>
        {
            table.HasCheckConstraint(
                "ck_ctpn_so_luong",
                "so_luong > 0");
        });

        builder.HasKey(x => new
        {
            x.PhieuNhapId,
            x.SanPhamId
        });

        builder.Property(x => x.PhieuNhapId)
            .HasColumnName("phieu_nhap_id")
            .HasColumnType("int unsigned")
            .ValueGeneratedNever();

        builder.Property(x => x.SanPhamId)
            .HasColumnName("san_pham_id")
            .HasColumnType("int unsigned")
            .ValueGeneratedNever();

        builder.Property(x => x.TenSanPham)
            .HasColumnName("ten_san_pham")
            .HasColumnType("varchar(200)")
            .IsRequired();

        builder.Property(x => x.DonViTinh)
            .HasColumnName("don_vi_tinh")
            .HasColumnType("varchar(20)")
            .IsRequired();

        builder.Property(x => x.SoLuong)
            .HasColumnName("so_luong")
            .HasColumnType("int unsigned");

        builder.Property(x => x.GiaNhap)
            .HasColumnName("gia_nhap")
            .HasColumnType("int unsigned");

        builder.Property(x => x.ThanhTien)
            .HasColumnName("thanh_tien")
            .HasColumnType("bigint unsigned")
            .HasComputedColumnSql(
                "so_luong * gia_nhap",
                stored: true);

        builder.HasIndex(x => x.SanPhamId)
            .HasDatabaseName("idx_ctpn_san_pham");

        builder.HasOne(x => x.PhieuNhap)
            .WithMany()
            .HasForeignKey(x => x.PhieuNhapId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("fk_ctpn_phieu");

        builder.HasOne(x => x.SanPham)
            .WithMany()
            .HasForeignKey(x => x.SanPhamId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("fk_ctpn_san_pham");
    }
}
