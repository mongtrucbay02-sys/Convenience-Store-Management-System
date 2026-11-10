using ConvenienceStore.Web.Modules.Products.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConvenienceStore.Web.Modules.Products.Configurations;

public sealed class SanPhamConfiguration
    : IEntityTypeConfiguration<SanPham>
{
    public void Configure(EntityTypeBuilder<SanPham> builder)
    {
        builder.ToTable("san_pham");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasColumnType("int unsigned")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.Ma)
            .HasColumnName("ma")
            .HasColumnType("varchar(20)")
            .IsRequired();

        builder.Property(x => x.TenSanPham)
            .HasColumnName("ten_san_pham")
            .HasColumnType("varchar(200)")
            .IsRequired();

        builder.Property(x => x.DanhMucId)
            .HasColumnName("danh_muc_id")
            .HasColumnType("int unsigned")
            .IsRequired();

        builder.Property(x => x.DonViTinh)
            .HasColumnName("don_vi_tinh")
            .HasColumnType("varchar(20)")
            .IsRequired();

        builder.Property(x => x.AnhUrl)
            .HasColumnName("anh_url")
            .HasColumnType("varchar(255)");

        builder.Property(x => x.MoTa)
            .HasColumnName("mo_ta")
            .HasColumnType("varchar(1000)");

        builder.Property(x => x.TrangThai)
            .HasColumnName("trang_thai")
            .HasColumnType("enum('DANG_KINH_DOANH','NGUNG_KINH_DOANH')")
            .HasDefaultValue("DANG_KINH_DOANH")
            .IsRequired();

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
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.NgayCapNhat)
            .HasColumnName("ngay_cap_nhat")
            .HasColumnType("datetime")
            .HasDefaultValueSql(
                "CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP")
            .ValueGeneratedOnAddOrUpdate();

        builder.HasIndex(x => x.Ma)
            .IsUnique()
            .HasDatabaseName("uq_san_pham_ma");

        builder.HasIndex(x => new { x.DanhMucId, x.TrangThai })
            .HasDatabaseName("idx_san_pham_danh_muc");

        builder.HasIndex(x => x.TenSanPham)
            .HasDatabaseName("idx_san_pham_ten");

        builder.HasOne(x => x.DanhMuc)
            .WithMany()
            .HasForeignKey(x => x.DanhMucId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_san_pham_danh_muc");

        builder.HasOne(x => x.NguoiTao)
            .WithMany()
            .HasForeignKey(x => x.NguoiTaoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_san_pham_nguoi_tao");
    }
}
