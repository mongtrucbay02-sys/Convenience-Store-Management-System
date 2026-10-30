using ConvenienceStore.Web.Modules.PointOfSale.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConvenienceStore.Web.Modules.PointOfSale.Configurations;

public sealed class GioHangConfiguration
    : IEntityTypeConfiguration<GioHang>
{
    public void Configure(EntityTypeBuilder<GioHang> builder)
    {
        builder.ToTable("gio_hang", table =>
        {
            table.HasCheckConstraint(
                "ck_gio_huy",
                "trang_thai <> 'DA_HUY' OR " +
                "(huy_luc IS NOT NULL AND ly_do_huy IS NOT NULL " +
                "AND gia_tri_khi_huy IS NOT NULL)");

            table.HasCheckConstraint(
                "ck_gio_tam_giu",
                "trang_thai <> 'TAM_GIU' OR tam_giu_luc IS NOT NULL");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasColumnType("int unsigned")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.ThuNganId)
            .HasColumnName("thu_ngan_id")
            .HasColumnType("int unsigned")
            .IsRequired();

        builder.Property(x => x.KhachHangId)
            .HasColumnName("khach_hang_id")
            .HasColumnType("int unsigned");

        builder.Property(x => x.TrangThai)
            .HasColumnName("trang_thai")
            .HasColumnType("enum('DANG_BAN','TAM_GIU','DA_HOAN_TAT','DA_HUY')")
            .HasDefaultValue("DANG_BAN")
            .IsRequired();

        builder.Property(x => x.GhiChuTamGiu)
            .HasColumnName("ghi_chu_tam_giu")
            .HasColumnType("varchar(100)");

        builder.Property(x => x.TamGiuLuc)
            .HasColumnName("tam_giu_luc")
            .HasColumnType("datetime");

        builder.Property(x => x.HoanTatLuc)
            .HasColumnName("hoan_tat_luc")
            .HasColumnType("datetime");

        builder.Property(x => x.HuyLuc)
            .HasColumnName("huy_luc")
            .HasColumnType("datetime");

        builder.Property(x => x.LyDoHuy)
            .HasColumnName("ly_do_huy")
            .HasColumnType("enum('THU_NGAN_HUY','HET_HAN_TAM_GIU')");

        builder.Property(x => x.GiaTriKhiHuy)
            .HasColumnName("gia_tri_khi_huy")
            .HasColumnType("bigint unsigned");

        builder.Property(x => x.PhienBan)
            .HasColumnName("phien_ban")
            .HasColumnType("int unsigned")
            .HasDefaultValue(1u)
            .IsConcurrencyToken()
            .IsRequired();

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

        builder.Property(x => x.DangBanCuaThuNgan)
            .HasColumnName("dang_ban_cua_thu_ngan")
            .HasColumnType("int unsigned")
            .HasComputedColumnSql(
                "IF(trang_thai = 'DANG_BAN', thu_ngan_id, NULL)",
                stored: true);

        builder.HasIndex(x => x.DangBanCuaThuNgan)
            .IsUnique()
            .HasDatabaseName("uq_gio_dang_ban");

        builder.HasIndex(x => new { x.ThuNganId, x.TrangThai })
            .HasDatabaseName("idx_gio_thu_ngan");

        builder.HasIndex(x => new { x.TrangThai, x.HuyLuc })
            .HasDatabaseName("idx_gio_huy");

        builder.HasIndex(x => x.KhachHangId)
            .HasDatabaseName("idx_gio_khach");

        builder.HasOne(x => x.ThuNgan)
            .WithMany()
            .HasForeignKey(x => x.ThuNganId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_gio_thu_ngan");

        builder.HasOne(x => x.KhachHang)
            .WithMany()
            .HasForeignKey(x => x.KhachHangId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_gio_khach");
    }
}