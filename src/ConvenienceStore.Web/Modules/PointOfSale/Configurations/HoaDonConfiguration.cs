using ConvenienceStore.Web.Modules.PointOfSale.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConvenienceStore.Web.Modules.PointOfSale.Configurations;

public sealed class HoaDonConfiguration : IEntityTypeConfiguration<HoaDon>
{
    public void Configure(EntityTypeBuilder<HoaDon> builder)
    {
        builder.ToTable("hoa_don", table =>
        {
            table.HasCheckConstraint(
                "ck_hd_tong",
                "tong_thanh_toan + tong_giam = tong_tien_hang");

            table.HasCheckConstraint(
                "ck_hd_tien_thua",
                "tien_khach_dua + tien_tru_hang_doi = tong_thanh_toan + tien_thua");

            table.HasCheckConstraint(
                "ck_hd_tru_doi",
                "tien_tru_hang_doi <= tong_thanh_toan");

            table.HasCheckConstraint(
                "ck_hd_diem",
                "khach_hang_id IS NOT NULL OR " +
                "(diem_cong = 0 AND so_tien_mot_diem IS NULL)");

            table.HasCheckConstraint(
                "ck_hd_huy",
                "trang_thai <> 'DA_HUY' OR " +
                "(nguoi_huy_id IS NOT NULL AND huy_luc IS NOT NULL " +
                "AND ly_do_huy IS NOT NULL)");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasColumnType("int unsigned")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.MaHoaDon)
            .HasColumnName("ma_hoa_don")
            .HasColumnType("varchar(20)")
            .IsRequired();

        builder.Property(x => x.GioHangId)
            .HasColumnName("gio_hang_id")
            .HasColumnType("int unsigned")
            .IsRequired();

        builder.Property(x => x.ThuNganId)
            .HasColumnName("thu_ngan_id")
            .HasColumnType("int unsigned")
            .IsRequired();

        builder.Property(x => x.KhachHangId)
            .HasColumnName("khach_hang_id")
            .HasColumnType("int unsigned");

        builder.Property(x => x.ThanhToanLuc)
            .HasColumnName("thanh_toan_luc")
            .HasColumnType("datetime")
            .IsRequired();

        builder.Property(x => x.TongTienHang)
            .HasColumnName("tong_tien_hang")
            .HasColumnType("bigint unsigned")
            .IsRequired();

        builder.Property(x => x.TongGiam)
            .HasColumnName("tong_giam")
            .HasColumnType("bigint unsigned")
            .HasDefaultValue(0UL)
            .IsRequired();

        builder.Property(x => x.TongThanhToan)
            .HasColumnName("tong_thanh_toan")
            .HasColumnType("bigint unsigned")
            .IsRequired();

        builder.Property(x => x.TienTruHangDoi)
            .HasColumnName("tien_tru_hang_doi")
            .HasColumnType("bigint unsigned")
            .HasDefaultValue(0UL)
            .IsRequired();

        builder.Property(x => x.TienKhachDua)
            .HasColumnName("tien_khach_dua")
            .HasColumnType("bigint unsigned")
            .IsRequired();

        builder.Property(x => x.TienThua)
            .HasColumnName("tien_thua")
            .HasColumnType("bigint unsigned")
            .IsRequired();

        builder.Property(x => x.SoTienMotDiem)
            .HasColumnName("so_tien_mot_diem")
            .HasColumnType("int unsigned");

        builder.Property(x => x.DiemCong)
            .HasColumnName("diem_cong")
            .HasColumnType("int unsigned")
            .HasDefaultValue(0u)
            .IsRequired();

        builder.Property(x => x.TrangThai)
            .HasColumnName("trang_thai")
            .HasColumnType("enum('DA_THANH_TOAN','DA_HUY')")
            .HasDefaultValue("DA_THANH_TOAN")
            .IsRequired();

        builder.Property(x => x.NguoiHuyId)
            .HasColumnName("nguoi_huy_id")
            .HasColumnType("int unsigned");

        builder.Property(x => x.HuyLuc)
            .HasColumnName("huy_luc")
            .HasColumnType("datetime");

        builder.Property(x => x.LyDoHuy)
            .HasColumnName("ly_do_huy")
            .HasColumnType("varchar(255)");

        builder.HasIndex(x => x.MaHoaDon)
            .IsUnique()
            .HasDatabaseName("uq_hoa_don_ma");

        builder.HasIndex(x => x.GioHangId)
            .IsUnique()
            .HasDatabaseName("uq_hoa_don_gio");

        builder.HasIndex(x => new { x.TrangThai, x.ThanhToanLuc })
            .HasDatabaseName("idx_hoa_don_tt_tg");

        builder.HasIndex(x => new { x.ThuNganId, x.ThanhToanLuc })
            .HasDatabaseName("idx_hoa_don_thu_ngan");

        builder.HasIndex(x => new { x.KhachHangId, x.ThanhToanLuc })
            .HasDatabaseName("idx_hoa_don_khach");

        builder.HasIndex(x => x.HuyLuc)
            .HasDatabaseName("idx_hoa_don_huy");

        builder.HasOne(x => x.GioHang)
            .WithMany()
            .HasForeignKey(x => x.GioHangId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_hd_gio");

        builder.HasOne(x => x.ThuNgan)
            .WithMany()
            .HasForeignKey(x => x.ThuNganId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_hd_thu_ngan");

        builder.HasOne(x => x.KhachHang)
            .WithMany()
            .HasForeignKey(x => x.KhachHangId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_hd_khach");

        builder.HasOne(x => x.NguoiHuy)
            .WithMany()
            .HasForeignKey(x => x.NguoiHuyId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_hd_nguoi_huy");
    }
}