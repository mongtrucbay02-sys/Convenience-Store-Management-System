using ConvenienceStore.Web.Modules.Products.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConvenienceStore.Web.Modules.Products.Configurations;

public sealed class DanhMucConfiguration
    : IEntityTypeConfiguration<DanhMuc>
{
    public void Configure(EntityTypeBuilder<DanhMuc> builder)
    {
        builder.ToTable("danh_muc");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasColumnType("int unsigned")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.Ten)
            .HasColumnName("ten")
            .HasColumnType("varchar(100)")
            .IsRequired();

        builder.Property(x => x.MoTa)
            .HasColumnName("mo_ta")
            .HasColumnType("varchar(255)");

        builder.Property(x => x.KhongDoiTra)
            .HasColumnName("khong_doi_tra")
            .HasColumnType("tinyint(1)")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.TrangThai)
            .HasColumnName("trang_thai")
            .HasColumnType("enum('HOAT_DONG','DA_AN')")
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

        builder.HasIndex(x => x.Ten)
            .IsUnique()
            .HasDatabaseName("uq_danh_muc_ten");
    }
}
