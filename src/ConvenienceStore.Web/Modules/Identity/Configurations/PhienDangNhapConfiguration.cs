using ConvenienceStore.Web.Modules.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConvenienceStore.Web.Modules.Identity.Configurations;

public sealed class PhienDangNhapConfiguration
    : IEntityTypeConfiguration<PhienDangNhap>
{
    public void Configure(EntityTypeBuilder<PhienDangNhap> builder)
    {
        builder.ToTable("phien_dang_nhap");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasColumnType("bigint unsigned")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.TaiKhoanId)
            .HasColumnName("tai_khoan_id")
            .HasColumnType("int unsigned")
            .IsRequired();

        builder.Property(x => x.TokenBam)
            .HasColumnName("token_bam")
            .HasColumnType("char(64)")
            .IsRequired();

        builder.Property(x => x.NgayTao)
            .HasColumnName("ngay_tao")
            .HasColumnType("datetime")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.HoatDongCuoi)
            .HasColumnName("hoat_dong_cuoi")
            .HasColumnType("datetime")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.HuyLuc)
            .HasColumnName("huy_luc")
            .HasColumnType("datetime");

        builder.HasIndex(x => x.TokenBam)
            .IsUnique()
            .HasDatabaseName("uq_phien_token");

        builder.HasIndex(x => new { x.TaiKhoanId, x.HuyLuc })
            .HasDatabaseName("idx_phien_tai_khoan");

        builder.HasOne(x => x.TaiKhoan)
            .WithMany()
            .HasForeignKey(x => x.TaiKhoanId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_phien_tai_khoan");
    }
}
