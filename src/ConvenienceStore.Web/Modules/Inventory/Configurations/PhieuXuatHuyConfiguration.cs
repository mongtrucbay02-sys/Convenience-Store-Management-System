using ConvenienceStore.Web.Modules.Inventory.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConvenienceStore.Web.Modules.Inventory.Configurations;

public sealed class PhieuXuatHuyConfiguration
    : IEntityTypeConfiguration<PhieuXuatHuy>
{
    public void Configure(EntityTypeBuilder<PhieuXuatHuy> builder)
    {
        builder.ToTable("phieu_xuat_huy", table =>
        {
            table.HasCheckConstraint(
                "ck_pxh_tu_choi",
                "trang_thai <> 'BI_TU_CHOI' OR ly_do_tu_choi IS NOT NULL");

            table.HasCheckConstraint(
                "ck_pxh_duyet",
                "trang_thai NOT IN ('BI_TU_CHOI','DA_XUAT_HUY') OR " +
                "(nguoi_duyet_id IS NOT NULL AND duyet_luc IS NOT NULL)");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasColumnType("int unsigned")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.MaPhieu)
            .HasColumnName("ma_phieu")
            .HasColumnType("varchar(20)")
            .IsRequired();

        builder.Property(x => x.TrangThai)
            .HasColumnName("trang_thai")
            .HasColumnType(
                "enum('NHAP','CHO_DUYET','BI_TU_CHOI','DA_XUAT_HUY')")
            .HasDefaultValue("NHAP")
            .IsRequired();

        builder.Property(x => x.GhiChu)
            .HasColumnName("ghi_chu")
            .HasColumnType("varchar(255)");

        builder.Property(x => x.NguoiLapId)
            .HasColumnName("nguoi_lap_id")
            .HasColumnType("int unsigned")
            .IsRequired();

        builder.Property(x => x.NgayLap)
            .HasColumnName("ngay_lap")
            .HasColumnType("datetime")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(x => x.GuiDuyetLuc)
            .HasColumnName("gui_duyet_luc")
            .HasColumnType("datetime");

        builder.Property(x => x.NguoiDuyetId)
            .HasColumnName("nguoi_duyet_id")
            .HasColumnType("int unsigned");

        builder.Property(x => x.DuyetLuc)
            .HasColumnName("duyet_luc")
            .HasColumnType("datetime");

        builder.Property(x => x.LyDoTuChoi)
            .HasColumnName("ly_do_tu_choi")
            .HasColumnType("varchar(255)");

        builder.Property(x => x.PhienBan)
            .HasColumnName("phien_ban")
            .HasColumnType("int unsigned")
            .HasDefaultValue(1u)
            .IsConcurrencyToken();

        builder.HasIndex(x => x.MaPhieu)
            .IsUnique()
            .HasDatabaseName("uq_phieu_xuat_huy_ma");

        builder.HasIndex(x => x.TrangThai)
            .HasDatabaseName("idx_phieu_xuat_huy_tt");

        builder.HasIndex(x => x.DuyetLuc)
            .HasDatabaseName("idx_phieu_xuat_huy_duyet");

        builder.HasOne(x => x.NguoiLap)
            .WithMany()
            .HasForeignKey(x => x.NguoiLapId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_pxh_nguoi_lap");

        builder.HasOne(x => x.NguoiDuyet)
            .WithMany()
            .HasForeignKey(x => x.NguoiDuyetId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_pxh_nguoi_duyet");
    }
}