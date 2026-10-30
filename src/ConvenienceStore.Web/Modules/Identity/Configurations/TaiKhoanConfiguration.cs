using ConvenienceStore.Web.Modules.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConvenienceStore.Web.Modules.Identity.Configurations;

public sealed class TaiKhoanConfiguration
    : IEntityTypeConfiguration<TaiKhoan>
{
    public void Configure(EntityTypeBuilder<TaiKhoan> builder)
    {
        builder.ToTable("tai_khoan", table =>
        {
            table.HasCheckConstraint(
                "ck_tai_khoan_ten",
                "REGEXP_LIKE(ten_dang_nhap, '^[A-Za-z0-9._]{3,50}$')");

            table.HasCheckConstraint(
                "ck_tai_khoan_sdt",
                "so_dien_thoai IS NULL OR REGEXP_LIKE(so_dien_thoai, '^0[0-9]{9}$')");

            table.HasCheckConstraint(
                "ck_tai_khoan_khoa",
                "trang_thai <> 'DA_KHOA' OR ly_do_khoa IS NOT NULL");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasColumnType("int unsigned")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.TenDangNhap)
            .HasColumnName("ten_dang_nhap")
            .HasColumnType("varchar(50)")
            .IsRequired();

        builder.Property(x => x.MatKhauBam)
            .HasColumnName("mat_khau_bam")
            .HasColumnType("varchar(255)")
            .IsRequired();

        builder.Property(x => x.HoTen)
            .HasColumnName("ho_ten")
            .HasColumnType("varchar(100)")
            .IsRequired();

        builder.Property(x => x.SoDienThoai)
            .HasColumnName("so_dien_thoai")
            .HasColumnType("varchar(10)");

        builder.Property(x => x.Email)
            .HasColumnName("email")
            .HasColumnType("varchar(100)");

        builder.Property(x => x.VaiTroId)
            .HasColumnName("vai_tro_id")
            .HasColumnType("tinyint unsigned")
            .IsRequired();

        builder.Property(x => x.TrangThai)
            .HasColumnName("trang_thai")
            .HasColumnType("enum('HOAT_DONG','DA_KHOA')")
            .HasDefaultValue("HOAT_DONG")
            .IsRequired();

        builder.Property(x => x.LyDoKhoa)
            .HasColumnName("ly_do_khoa")
            .HasColumnType("varchar(255)");

        builder.Property(x => x.BatBuocDoiMk)
            .HasColumnName("bat_buoc_doi_mk")
            .HasColumnType("tinyint(1)")
            .HasDefaultValue(true);

        builder.Property(x => x.SoLanSai)
            .HasColumnName("so_lan_sai")
            .HasColumnType("tinyint unsigned")
            .HasDefaultValue((byte)0);

        builder.Property(x => x.KhoaTamDen)
            .HasColumnName("khoa_tam_den")
            .HasColumnType("datetime");

        builder.Property(x => x.LanDangNhapCuoi)
            .HasColumnName("lan_dang_nhap_cuoi")
            .HasColumnType("datetime");

        builder.Property(x => x.PhienBan)
            .HasColumnName("phien_ban")
            .HasColumnType("int unsigned")
            .HasDefaultValue(1u)
            .IsConcurrencyToken();

        builder.Property(x => x.NgayTao)
            .HasColumnName("ngay_tao")
            .HasColumnType("datetime")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.NgayCapNhat)
            .HasColumnName("ngay_cap_nhat")
            .HasColumnType("datetime")
            .HasDefaultValueSql("CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP")
            .ValueGeneratedOnAddOrUpdate();

        builder.HasIndex(x => x.TenDangNhap)
            .IsUnique()
            .HasDatabaseName("uq_tai_khoan_ten");

        builder.HasIndex(x => x.SoDienThoai)
            .IsUnique()
            .HasDatabaseName("uq_tai_khoan_sdt");

        builder.HasIndex(x => x.Email)
            .IsUnique()
            .HasDatabaseName("uq_tai_khoan_email");

        builder.HasIndex(x => new { x.VaiTroId, x.TrangThai })
            .HasDatabaseName("idx_tai_khoan_vai_tro");

        builder.HasOne(x => x.VaiTro)
            .WithMany()
            .HasForeignKey(x => x.VaiTroId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tai_khoan_vai_tro");
    }
}
