using ConvenienceStore.Web.Modules.Customers.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConvenienceStore.Web.Modules.Customers.Configurations;

public sealed class KhachHangConfiguration
    : IEntityTypeConfiguration<KhachHang>
{
    public void Configure(EntityTypeBuilder<KhachHang> builder)
    {
        builder.ToTable("khach_hang", table =>
        {
            table.HasCheckConstraint(
                "ck_kh_sdt",
                "so_dien_thoai IS NULL OR " +
                "REGEXP_LIKE(so_dien_thoai, '^0[0-9]{9}$')");

            table.HasCheckConstraint(
                "ck_kh_hoat_dong",
                "trang_thai <> 'HOAT_DONG' OR " +
                "(ho_ten IS NOT NULL AND so_dien_thoai IS NOT NULL)");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasColumnType("int unsigned")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.HoTen)
            .HasColumnName("ho_ten")
            .HasColumnType("varchar(100)");

        builder.Property(x => x.SoDienThoai)
            .HasColumnName("so_dien_thoai")
            .HasColumnType("varchar(10)");

        builder.Property(x => x.Email)
            .HasColumnName("email")
            .HasColumnType("varchar(100)");

        builder.Property(x => x.NgaySinh)
            .HasColumnName("ngay_sinh")
            .HasColumnType("date");

        builder.Property(x => x.GioiTinh)
            .HasColumnName("gioi_tinh")
            .HasColumnType("enum('NAM','NU','KHAC')");

        builder.Property(x => x.DiemHienCo)
            .HasColumnName("diem_hien_co")
            .HasColumnType("int unsigned")
            .HasDefaultValue(0u)
            .IsRequired();

        builder.Property(x => x.TrangThai)
            .HasColumnName("trang_thai")
            .HasColumnType("enum('HOAT_DONG','DA_AN')")
            .HasDefaultValue("HOAT_DONG")
            .IsRequired();

        builder.Property(x => x.XoaDuLieuLuc)
            .HasColumnName("xoa_du_lieu_luc")
            .HasColumnType("datetime");

        builder.Property(x => x.NguoiTaoId)
            .HasColumnName("nguoi_tao_id")
            .HasColumnType("int unsigned")
            .IsRequired();

        builder.Property(x => x.PhienBan)
            .HasColumnName("phien_ban")
            .HasColumnType("int unsigned")
            .HasDefaultValue(1u)
            .IsConcurrencyToken();

        builder.Property(x => x.NgayTao)
            .HasColumnName("ngay_tao")
            .HasColumnType("datetime")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(x => x.NgayCapNhat)
            .HasColumnName("ngay_cap_nhat")
            .HasColumnType("datetime")
            .HasDefaultValueSql(
                "CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP")
            .ValueGeneratedOnAddOrUpdate()
            .IsRequired();

        builder.HasIndex(x => x.SoDienThoai)
            .IsUnique()
            .HasDatabaseName("uq_khach_hang_sdt");

        builder.HasIndex(x => x.HoTen)
            .HasDatabaseName("idx_khach_hang_ten");

        builder.HasOne(x => x.NguoiTao)
            .WithMany()
            .HasForeignKey(x => x.NguoiTaoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_kh_nguoi_tao");
    }
}