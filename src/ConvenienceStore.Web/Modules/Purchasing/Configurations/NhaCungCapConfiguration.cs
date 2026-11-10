using ConvenienceStore.Web.Modules.Purchasing.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConvenienceStore.Web.Modules.Purchasing.Configurations;

public sealed class NhaCungCapConfiguration
    : IEntityTypeConfiguration<NhaCungCap>
{
    public void Configure(EntityTypeBuilder<NhaCungCap> builder)
    {
        builder.ToTable("nha_cung_cap", table =>
        {
            table.HasCheckConstraint(
                "ck_ncc_sdt",
                "REGEXP_LIKE(so_dien_thoai, '^0[0-9]{9,10}$')");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasColumnType("int unsigned")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.Ma)
            .HasColumnName("ma")
            .HasColumnType("varchar(20)")
            .IsRequired();

        builder.Property(x => x.Ten)
            .HasColumnName("ten")
            .HasColumnType("varchar(200)")
            .IsRequired();

        builder.Property(x => x.NguoiLienHe)
            .HasColumnName("nguoi_lien_he")
            .HasColumnType("varchar(100)");

        builder.Property(x => x.SoDienThoai)
            .HasColumnName("so_dien_thoai")
            .HasColumnType("varchar(11)")
            .IsRequired();

        builder.Property(x => x.Email)
            .HasColumnName("email")
            .HasColumnType("varchar(100)");

        builder.Property(x => x.DiaChi)
            .HasColumnName("dia_chi")
            .HasColumnType("varchar(255)");

        builder.Property(x => x.MaSoThue)
            .HasColumnName("ma_so_thue")
            .HasColumnType("varchar(14)");

        builder.Property(x => x.GhiChu)
            .HasColumnName("ghi_chu")
            .HasColumnType("varchar(500)");

        builder.Property(x => x.TrangThai)
            .HasColumnName("trang_thai")
            .HasColumnType("enum('HOAT_DONG','NGUNG_HOP_TAC')")
            .HasDefaultValue("HOAT_DONG")
            .IsRequired();

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
            .HasDefaultValueSql(
                "CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP")
            .ValueGeneratedOnAddOrUpdate();

        builder.HasIndex(x => x.Ma)
            .IsUnique()
            .HasDatabaseName("uq_ncc_ma");

        builder.HasIndex(x => x.SoDienThoai)
            .IsUnique()
            .HasDatabaseName("uq_ncc_sdt");

        builder.HasIndex(x => x.Ten)
            .HasDatabaseName("idx_ncc_ten");
    }
}
