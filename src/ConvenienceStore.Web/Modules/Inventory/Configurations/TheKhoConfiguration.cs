using ConvenienceStore.Web.Modules.Inventory.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConvenienceStore.Web.Modules.Inventory.Configurations;

public sealed class TheKhoConfiguration
    : IEntityTypeConfiguration<TheKho>
{
    public void Configure(EntityTypeBuilder<TheKho> builder)
    {
        builder.ToTable("the_kho", table =>
        {
            table.HasCheckConstraint(
                "ck_the_kho_khac_0",
                "so_luong_thay_doi <> 0");

            table.HasCheckConstraint(
                "ck_the_kho_ton_sau",
                "ton_sau >= 0");

            table.HasCheckConstraint(
                "ck_the_kho_huong",
                @"(
                    (loai_bien_dong IN ('NHAP_HANG','HUY_HOA_DON','TRA_HANG')
                     AND so_luong_thay_doi > 0)
                    OR
                    (loai_bien_dong IN ('BAN_HANG','XUAT_HUY')
                     AND so_luong_thay_doi < 0)
                    OR
                    loai_bien_dong = 'KIEM_KE'
                  )");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasColumnType("bigint unsigned")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.SanPhamId)
            .HasColumnName("san_pham_id")
            .HasColumnType("int unsigned")
            .IsRequired();

        builder.Property(x => x.LoaiBienDong)
            .HasColumnName("loai_bien_dong")
            .HasColumnType(
                "enum('NHAP_HANG','BAN_HANG','HUY_HOA_DON','TRA_HANG','KIEM_KE','XUAT_HUY')")
            .IsRequired();

        builder.Property(x => x.MaChungTu)
            .HasColumnName("ma_chung_tu")
            .HasColumnType("varchar(20)")
            .IsRequired();

        builder.Property(x => x.ChungTuId)
            .HasColumnName("chung_tu_id")
            .HasColumnType("int unsigned")
            .IsRequired();

        builder.Property(x => x.SoLuongThayDoi)
            .HasColumnName("so_luong_thay_doi")
            .HasColumnType("int")
            .IsRequired();

        builder.Property(x => x.TonSau)
            .HasColumnName("ton_sau")
            .HasColumnType("int")
            .IsRequired();

        builder.Property(x => x.GiaVonApDung)
            .HasColumnName("gia_von_ap_dung")
            .HasColumnType("decimal(15,2)")
            .IsRequired();

        builder.Property(x => x.GiaVonSau)
            .HasColumnName("gia_von_sau")
            .HasColumnType("decimal(15,2)")
            .IsRequired();

        builder.Property(x => x.NguoiThucHienId)
            .HasColumnName("nguoi_thuc_hien_id")
            .HasColumnType("int unsigned")
            .IsRequired();

        builder.Property(x => x.ThoiDiem)
            .HasColumnName("thoi_diem")
            .HasColumnType("datetime")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.HasIndex(x => new { x.SanPhamId, x.ThoiDiem })
            .HasDatabaseName("idx_the_kho_sp_tg");

        builder.HasIndex(x => x.ThoiDiem)
            .HasDatabaseName("idx_the_kho_tg");

        builder.HasIndex(x => new { x.LoaiBienDong, x.ChungTuId })
            .HasDatabaseName("idx_the_kho_chung_tu");

        builder.HasOne(x => x.SanPham)
            .WithMany()
            .HasForeignKey(x => x.SanPhamId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_the_kho_san_pham");

        builder.HasOne(x => x.NguoiThucHien)
            .WithMany()
            .HasForeignKey(x => x.NguoiThucHienId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_the_kho_nguoi");
    }
}