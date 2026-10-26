using ConvenienceStore.Web.Modules.Identity.Entities;
using ConvenienceStore.Web.Modules.Purchasing.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConvenienceStore.Web.Modules.Purchasing.Configurations;

public sealed class PhieuNhapConfiguration
    : IEntityTypeConfiguration<PhieuNhap>
{
    public void Configure(EntityTypeBuilder<PhieuNhap> builder)
    {
        builder.ToTable("phieu_nhap", table =>
        {
            table.HasCheckConstraint(
                "ck_pn_xac_nhan",
                "trang_thai <> 'DA_NHAP_KHO' OR (nguoi_xac_nhan_id IS NOT NULL AND xac_nhan_luc IS NOT NULL)");

            table.HasCheckConstraint(
                "ck_pn_huy",
                "trang_thai <> 'DA_HUY' OR (nguoi_huy_id IS NOT NULL AND huy_luc IS NOT NULL AND ly_do_huy IS NOT NULL)");
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

        builder.Property(x => x.NhaCungCapId)
            .HasColumnName("nha_cung_cap_id")
            .HasColumnType("int unsigned");

        builder.Property(x => x.SoChungTuNcc)
            .HasColumnName("so_chung_tu_ncc")
            .HasColumnType("varchar(50)");

        builder.Property(x => x.NgayNhap)
            .HasColumnName("ngay_nhap")
            .HasColumnType("date")
            .HasConversion(
                date => date.ToDateTime(TimeOnly.MinValue),
                dateTime => DateOnly.FromDateTime(dateTime));

        builder.Property(x => x.TrangThai)
            .HasColumnName("trang_thai")
            .HasColumnType("enum('NHAP','DA_NHAP_KHO','DA_HUY')")
            .HasDefaultValue("NHAP")
            .IsRequired();

        builder.Property(x => x.TongTien)
            .HasColumnName("tong_tien")
            .HasColumnType("bigint unsigned")
            .HasDefaultValue(0UL);

        builder.Property(x => x.GhiChu)
            .HasColumnName("ghi_chu")
            .HasColumnType("varchar(255)");

        builder.Property(x => x.NguoiLapId)
            .HasColumnName("nguoi_lap_id")
            .HasColumnType("int unsigned");

        builder.Property(x => x.NgayLap)
            .HasColumnName("ngay_lap")
            .HasColumnType("datetime")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.NguoiXacNhanId)
            .HasColumnName("nguoi_xac_nhan_id")
            .HasColumnType("int unsigned");

        builder.Property(x => x.XacNhanLuc)
            .HasColumnName("xac_nhan_luc")
            .HasColumnType("datetime");

        builder.Property(x => x.NguoiHuyId)
            .HasColumnName("nguoi_huy_id")
            .HasColumnType("int unsigned");

        builder.Property(x => x.HuyLuc)
            .HasColumnName("huy_luc")
            .HasColumnType("datetime");

        builder.Property(x => x.LyDoHuy)
            .HasColumnName("ly_do_huy")
            .HasColumnType("varchar(255)");

        builder.Property(x => x.PhienBan)
            .HasColumnName("phien_ban")
            .HasColumnType("int unsigned")
            .HasDefaultValue(1u)
            .IsConcurrencyToken();

        builder.Property(x => x.NgayCapNhat)
            .HasColumnName("ngay_cap_nhat")
            .HasColumnType("datetime")
            .HasDefaultValueSql(
                "CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP")
            .ValueGeneratedOnAddOrUpdate();

        builder.HasIndex(x => x.MaPhieu)
            .IsUnique()
            .HasDatabaseName("uq_phieu_nhap_ma");

        builder.HasIndex(x => new { x.TrangThai, x.NgayLap })
            .HasDatabaseName("idx_phieu_nhap_tt");

        builder.HasIndex(x => new { x.NhaCungCapId, x.XacNhanLuc })
            .HasDatabaseName("idx_phieu_nhap_ncc");

        builder.HasIndex(x => x.XacNhanLuc)
            .HasDatabaseName("idx_phieu_nhap_xac_nhan");

        builder.HasOne(x => x.NhaCungCap)
            .WithMany()
            .HasForeignKey(x => x.NhaCungCapId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("fk_pn_ncc");

        builder.HasOne(x => x.NguoiLap)
            .WithMany()
            .HasForeignKey(x => x.NguoiLapId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("fk_pn_nguoi_lap");

        builder.HasOne(x => x.NguoiXacNhan)
            .WithMany()
            .HasForeignKey(x => x.NguoiXacNhanId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("fk_pn_nguoi_xac_nhan");

        builder.HasOne(x => x.NguoiHuy)
            .WithMany()
            .HasForeignKey(x => x.NguoiHuyId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("fk_pn_nguoi_huy");
    }
}

