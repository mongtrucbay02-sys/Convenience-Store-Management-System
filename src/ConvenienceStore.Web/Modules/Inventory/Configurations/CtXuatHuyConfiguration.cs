using ConvenienceStore.Web.Modules.Inventory.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConvenienceStore.Web.Modules.Inventory.Configurations;

public sealed class CtXuatHuyConfiguration
    : IEntityTypeConfiguration<CtXuatHuy>
{
    public void Configure(EntityTypeBuilder<CtXuatHuy> builder)
    {
        builder.ToTable("ct_xuat_huy", table =>
        {
            table.HasCheckConstraint(
                "ck_ctxh_so_luong",
                "so_luong > 0");

            table.HasCheckConstraint(
                "ck_ctxh_khac",
                "ly_do <> 'KHAC' OR ghi_chu_ly_do IS NOT NULL");
        });

        builder.HasKey(x => new
        {
            x.PhieuXuatHuyId,
            x.SanPhamId
        });

        builder.Property(x => x.PhieuXuatHuyId)
            .HasColumnName("phieu_xuat_huy_id")
            .HasColumnType("int unsigned")
            .ValueGeneratedNever();

        builder.Property(x => x.SanPhamId)
            .HasColumnName("san_pham_id")
            .HasColumnType("int unsigned")
            .ValueGeneratedNever();

        builder.Property(x => x.SoLuong)
            .HasColumnName("so_luong")
            .HasColumnType("int unsigned")
            .IsRequired();

        builder.Property(x => x.LyDo)
            .HasColumnName("ly_do")
            .HasColumnType("enum('HONG','HET_HAN','MAT','KHAC')")
            .IsRequired();

        builder.Property(x => x.GhiChuLyDo)
            .HasColumnName("ghi_chu_ly_do")
            .HasColumnType("varchar(255)");

        builder.Property(x => x.GiaVonLucDuyet)
            .HasColumnName("gia_von_luc_duyet")
            .HasColumnType("decimal(15,2)");

        builder.HasIndex(x => x.SanPhamId)
            .HasDatabaseName("idx_ctxh_san_pham");

        builder.HasOne(x => x.PhieuXuatHuy)
            .WithMany()
            .HasForeignKey(x => x.PhieuXuatHuyId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_ctxh_phieu");

        builder.HasOne(x => x.SanPham)
            .WithMany()
            .HasForeignKey(x => x.SanPhamId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_ctxh_san_pham");
    }
}