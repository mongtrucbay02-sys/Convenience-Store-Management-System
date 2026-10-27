using ConvenienceStore.Web.Modules.Customers.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConvenienceStore.Web.Modules.Customers.Configurations;

public sealed class LichSuDiemConfiguration
    : IEntityTypeConfiguration<LichSuDiem>
{
    public void Configure(EntityTypeBuilder<LichSuDiem> builder)
    {
        builder.ToTable("lich_su_diem", table =>
        {
            table.HasCheckConstraint(
                "ck_lsd_khac_0",
                "so_diem <> 0");

            table.HasCheckConstraint(
                "ck_lsd_huong",
                "(loai = 'TICH_DIEM' AND so_diem > 0 AND hoa_don_id IS NOT NULL) " +
                "OR (loai = 'THU_HOI_TRA_HANG' AND so_diem < 0 " +
                "AND phieu_doi_tra_id IS NOT NULL) " +
                "OR (loai = 'THU_HOI_HUY_HOA_DON' AND so_diem < 0 " +
                "AND hoa_don_id IS NOT NULL) " +
                "OR (loai = 'DIEU_CHINH' AND ly_do IS NOT NULL)");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasColumnType("bigint unsigned")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.KhachHangId)
            .HasColumnName("khach_hang_id")
            .HasColumnType("int unsigned")
            .IsRequired();

        builder.Property(x => x.Loai)
            .HasColumnName("loai")
            .HasColumnType(
                "enum('TICH_DIEM','THU_HOI_TRA_HANG'," +
                "'THU_HOI_HUY_HOA_DON','DIEU_CHINH')")
            .IsRequired();

        builder.Property(x => x.SoDiem)
            .HasColumnName("so_diem")
            .HasColumnType("int")
            .IsRequired();

        builder.Property(x => x.DiemSau)
            .HasColumnName("diem_sau")
            .HasColumnType("int unsigned")
            .IsRequired();

        builder.Property(x => x.HoaDonId)
            .HasColumnName("hoa_don_id")
            .HasColumnType("int unsigned");

        builder.Property(x => x.PhieuDoiTraId)
            .HasColumnName("phieu_doi_tra_id")
            .HasColumnType("int unsigned");

        builder.Property(x => x.LyDo)
            .HasColumnName("ly_do")
            .HasColumnType("varchar(255)");

        builder.Property(x => x.NguoiThucHienId)
            .HasColumnName("nguoi_thuc_hien_id")
            .HasColumnType("int unsigned")
            .IsRequired();

        builder.Property(x => x.ThoiDiem)
            .HasColumnName("thoi_diem")
            .HasColumnType("datetime")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.HasIndex(x => new { x.KhachHangId, x.ThoiDiem })
            .HasDatabaseName("idx_lsd_khach_tg");

        builder.HasIndex(x => x.HoaDonId)
            .HasDatabaseName("idx_lsd_hoa_don");

        builder.HasOne(x => x.KhachHang)
            .WithMany()
            .HasForeignKey(x => x.KhachHangId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_lsd_khach");

        builder.HasOne(x => x.HoaDon)
            .WithMany()
            .HasForeignKey(x => x.HoaDonId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_lsd_hd");

        builder.HasOne(x => x.PhieuDoiTra)
            .WithMany()
            .HasForeignKey(x => x.PhieuDoiTraId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_lsd_pdt");

        builder.HasOne(x => x.NguoiThucHien)
            .WithMany()
            .HasForeignKey(x => x.NguoiThucHienId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_lsd_nguoi");

        // BR-KH-02 append-only triggers will be installed
        // explicitly in the migration.
    }
}
