using ConvenienceStore.Web.Modules.PointOfSale.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConvenienceStore.Web.Modules.PointOfSale.Configurations;

public sealed class CtGioHangConfiguration
    : IEntityTypeConfiguration<CtGioHang>
{
    public void Configure(EntityTypeBuilder<CtGioHang> builder)
    {
        builder.ToTable("ct_gio_hang", table =>
        {
            table.HasCheckConstraint(
                "ck_ctgh_so_luong",
                "so_luong > 0");

            table.HasCheckConstraint(
                "ck_ctgh_giam",
                "giam_don_vi <= don_gia AND giam_don_vi % 100 = 0");

            table.HasCheckConstraint(
                "ck_ctgh_km",
                "khuyen_mai_id IS NOT NULL OR giam_don_vi = 0");
        });

        builder.HasKey(x => new { x.GioHangId, x.SanPhamId });

        builder.Property(x => x.GioHangId)
            .HasColumnName("gio_hang_id")
            .HasColumnType("int unsigned")
            .IsRequired();

        builder.Property(x => x.SanPhamId)
            .HasColumnName("san_pham_id")
            .HasColumnType("int unsigned")
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

        builder.Property(x => x.ThemLuc)
            .HasColumnName("them_luc")
            .HasColumnType("datetime")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.HasIndex(x => x.SanPhamId)
            .HasDatabaseName("idx_ctgh_san_pham");

        builder.HasOne(x => x.GioHang)
            .WithMany()
            .HasForeignKey(x => x.GioHangId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_ctgh_gio");

        builder.HasOne(x => x.SanPham)
            .WithMany()
            .HasForeignKey(x => x.SanPhamId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_ctgh_san_pham");

        builder.HasOne(x => x.KhuyenMai)
            .WithMany()
            .HasForeignKey(x => x.KhuyenMaiId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_ctgh_km");
    }
}