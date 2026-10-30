using ConvenienceStore.Web.Modules.Promotions.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConvenienceStore.Web.Modules.Promotions.Configurations;

public sealed class KhuyenMaiSanPhamConfiguration
    : IEntityTypeConfiguration<KhuyenMaiSanPham>
{
    public void Configure(EntityTypeBuilder<KhuyenMaiSanPham> builder)
    {
        builder.ToTable("khuyen_mai_san_pham");

        builder.HasKey(x => new { x.ChuongTrinhId, x.SanPhamId });

        builder.Property(x => x.ChuongTrinhId)
            .HasColumnName("chuong_trinh_id")
            .HasColumnType("int unsigned")
            .IsRequired();

        builder.Property(x => x.SanPhamId)
            .HasColumnName("san_pham_id")
            .HasColumnType("int unsigned")
            .IsRequired();

        builder.HasIndex(x => x.SanPhamId)
            .HasDatabaseName("idx_kmsp_san_pham");

        builder.HasOne(x => x.ChuongTrinh)
            .WithMany()
            .HasForeignKey(x => x.ChuongTrinhId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_kmsp_ct");

        builder.HasOne(x => x.SanPham)
            .WithMany()
            .HasForeignKey(x => x.SanPhamId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_kmsp_sp");
    }
}