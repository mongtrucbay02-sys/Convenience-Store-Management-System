using ConvenienceStore.Web.Modules.PointOfSale.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConvenienceStore.Web.Modules.PointOfSale.Configurations;

public sealed class CtHoaDonConfiguration
    : IEntityTypeConfiguration<CtHoaDon>
{
    public void Configure(EntityTypeBuilder<CtHoaDon> builder)
    {
        builder.ToTable("ct_hoa_don", table =>
        {
            table.HasCheckConstraint(
                "ck_cthd_so_luong",
                "so_luong > 0");

            table.HasCheckConstraint(
                "ck_cthd_giam",
                "giam_don_vi <= don_gia AND giam_don_vi % 100 = 0");

            table.HasCheckConstraint(
                "ck_cthd_km",
                "khuyen_mai_id IS NOT NULL OR giam_don_vi = 0");
        });

        builder.HasKey(x => new { x.HoaDonId, x.SanPhamId });

        builder.Property(x => x.HoaDonId)
            .HasColumnName("hoa_don_id")
            .HasColumnType("int unsigned")
            .IsRequired();

        builder.Property(x => x.SanPhamId)
            .HasColumnName("san_pham_id")
            .HasColumnType("int unsigned")
            .IsRequired();

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
            .HasColumnType("int unsigned")
            .IsRequired();

        builder.Property(x => x.DonGia)
            .HasColumnName("don_gia")
            .HasColumnType("int unsigned")
            .IsRequired();

        builder.Property(x => x.KhuyenMaiId)
            .HasColumnName("khuyen_mai_id")
            .HasColumnType("int unsigned");

        builder.Property(x => x.GiamDonVi)
            .HasColumnName("giam_don_vi")
            .HasColumnType("int unsigned")
            .HasDefaultValue(0u)
            .IsRequired();

        builder.Property(x => x.GiaVon)
            .HasColumnName("gia_von")
            .HasColumnType("decimal(15,2)")
            .IsRequired();

        builder.Property(x => x.ThanhTien)
            .HasColumnName("thanh_tien")
            .HasColumnType("bigint unsigned")
            .HasComputedColumnSql(
                "so_luong * (don_gia - giam_don_vi)",
                stored: true)
            .IsRequired();

        builder.HasIndex(x => x.SanPhamId)
            .HasDatabaseName("idx_cthd_san_pham");

        builder.HasIndex(x => x.KhuyenMaiId)
            .HasDatabaseName("idx_cthd_km");

        builder.HasOne(x => x.HoaDon)
            .WithMany()
            .HasForeignKey(x => x.HoaDonId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_cthd_hd");

        builder.HasOne(x => x.SanPham)
            .WithMany()
            .HasForeignKey(x => x.SanPhamId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_cthd_san_pham");

        builder.HasOne(x => x.KhuyenMai)
            .WithMany()
            .HasForeignKey(x => x.KhuyenMaiId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_cthd_km");
    }
}