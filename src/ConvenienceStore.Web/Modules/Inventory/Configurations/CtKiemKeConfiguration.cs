using ConvenienceStore.Web.Modules.Inventory.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConvenienceStore.Web.Modules.Inventory.Configurations;

public sealed class CtKiemKeConfiguration
    : IEntityTypeConfiguration<CtKiemKe>
{
    public void Configure(EntityTypeBuilder<CtKiemKe> builder)
    {
        builder.ToTable("ct_kiem_ke", table =>
        {
            table.HasCheckConstraint(
                "ck_ctkk_thuc_te",
                "so_luong_thuc_te IS NULL OR so_luong_thuc_te >= 0");
        });

        builder.HasKey(x => new
        {
            x.PhieuKiemKeId,
            x.SanPhamId
        });

        builder.Property(x => x.PhieuKiemKeId)
            .HasColumnName("phieu_kiem_ke_id")
            .HasColumnType("int unsigned")
            .ValueGeneratedNever();

        builder.Property(x => x.SanPhamId)
            .HasColumnName("san_pham_id")
            .HasColumnType("int unsigned")
            .ValueGeneratedNever();

        builder.Property(x => x.TonLucLap)
            .HasColumnName("ton_luc_lap")
            .HasColumnType("int")
            .IsRequired();

        builder.Property(x => x.SoLuongThucTe)
            .HasColumnName("so_luong_thuc_te")
            .HasColumnType("int");

        builder.Property(x => x.LyDoChenhLech)
            .HasColumnName("ly_do_chenh_lech")
            .HasColumnType("varchar(255)");

        builder.Property(x => x.ChenhLechLucDuyet)
            .HasColumnName("chenh_lech_luc_duyet")
            .HasColumnType("int");

        builder.Property(x => x.GiaVonLucDuyet)
            .HasColumnName("gia_von_luc_duyet")
            .HasColumnType("decimal(15,2)");

        builder.HasIndex(x => x.SanPhamId)
            .HasDatabaseName("idx_ctkk_san_pham");

        builder.HasOne(x => x.PhieuKiemKe)
            .WithMany()
            .HasForeignKey(x => x.PhieuKiemKeId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_ctkk_phieu");

        builder.HasOne(x => x.SanPham)
            .WithMany()
            .HasForeignKey(x => x.SanPhamId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_ctkk_san_pham");
    }
}