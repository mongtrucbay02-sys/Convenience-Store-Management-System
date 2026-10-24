using ConvenienceStore.Web.Modules.Products.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConvenienceStore.Web.Modules.Products.Configurations;

public sealed class LichSuGiaConfiguration
    : IEntityTypeConfiguration<LichSuGia>
{
    public void Configure(EntityTypeBuilder<LichSuGia> builder)
    {
        builder.ToTable("lich_su_gia", table =>
        {
            table.HasCheckConstraint(
                "ck_lsg_gia",
                "gia_ban > 0 AND gia_ban % 100 = 0");

            table.HasCheckConstraint(
                "ck_lsg_huy",
                "(huy_luc IS NULL) = (nguoi_huy_id IS NULL)");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasColumnType("int unsigned")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.SanPhamId)
            .HasColumnName("san_pham_id")
            .HasColumnType("int unsigned")
            .IsRequired();

        builder.Property(x => x.GiaBan)
            .HasColumnName("gia_ban")
            .HasColumnType("int unsigned")
            .IsRequired();

        builder.Property(x => x.HieuLucTu)
            .HasColumnName("hieu_luc_tu")
            .HasColumnType("datetime")
            .IsRequired();

        builder.Property(x => x.NguoiTaoId)
            .HasColumnName("nguoi_tao_id")
            .HasColumnType("int unsigned")
            .IsRequired();

        builder.Property(x => x.NgayTao)
            .HasColumnName("ngay_tao")
            .HasColumnType("datetime")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.HuyLuc)
            .HasColumnName("huy_luc")
            .HasColumnType("datetime");

        builder.Property(x => x.NguoiHuyId)
            .HasColumnName("nguoi_huy_id")
            .HasColumnType("int unsigned");

        builder.HasIndex(x => new { x.SanPhamId, x.HieuLucTu })
            .HasDatabaseName("idx_lsg_san_pham_hieu_luc");

        builder.HasOne(x => x.SanPham)
            .WithMany()
            .HasForeignKey(x => x.SanPhamId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_lsg_san_pham");

        builder.HasOne(x => x.NguoiTao)
            .WithMany()
            .HasForeignKey(x => x.NguoiTaoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_lsg_nguoi_tao");

        builder.HasOne(x => x.NguoiHuy)
            .WithMany()
            .HasForeignKey(x => x.NguoiHuyId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_lsg_nguoi_huy");
    }
}
