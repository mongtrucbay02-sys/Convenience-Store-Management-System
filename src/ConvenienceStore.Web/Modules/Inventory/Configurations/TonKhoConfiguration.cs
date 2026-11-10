using ConvenienceStore.Web.Modules.Inventory.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConvenienceStore.Web.Modules.Inventory.Configurations;

public sealed class TonKhoConfiguration
    : IEntityTypeConfiguration<TonKho>
{
    public void Configure(EntityTypeBuilder<TonKho> builder)
    {
        builder.ToTable("ton_kho", table =>
        {
            table.HasCheckConstraint(
                "ck_ton_khong_am", "so_luong_ton >= 0");

            table.HasCheckConstraint(
                "ck_gia_von_khong_am", "gia_von_binh_quan >= 0");

            table.HasCheckConstraint(
                "ck_nguong_khong_am", "nguong_toi_thieu >= 0");
        });

        builder.HasKey(x => x.SanPhamId);

        builder.Property(x => x.SanPhamId)
            .HasColumnName("san_pham_id")
            .HasColumnType("int unsigned")
            .ValueGeneratedNever();

        builder.Property(x => x.SoLuongTon)
            .HasColumnName("so_luong_ton")
            .HasColumnType("int")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.GiaVonBinhQuan)
            .HasColumnName("gia_von_binh_quan")
            .HasColumnType("decimal(15,2)")
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(x => x.NguongToiThieu)
            .HasColumnName("nguong_toi_thieu")
            .HasColumnType("int")
            .HasDefaultValue(5)
            .IsRequired();

        builder.Property(x => x.NgayCapNhat)
            .HasColumnName("ngay_cap_nhat")
            .HasColumnType("datetime")
            .HasDefaultValueSql(
                "CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP")
            .ValueGeneratedOnAddOrUpdate()
            .IsRequired();

        builder.HasOne(x => x.SanPham)
            .WithOne()
            .HasForeignKey<TonKho>(x => x.SanPhamId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_ton_kho_san_pham");
    }
}