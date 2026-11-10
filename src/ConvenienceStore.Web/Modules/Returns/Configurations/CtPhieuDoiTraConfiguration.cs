using ConvenienceStore.Web.Modules.Returns.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConvenienceStore.Web.Modules.Returns.Configurations;

public sealed class CtPhieuDoiTraConfiguration
    : IEntityTypeConfiguration<CtPhieuDoiTra>
{
    public void Configure(EntityTypeBuilder<CtPhieuDoiTra> builder)
    {
        builder.ToTable("ct_phieu_doi_tra", table =>
        {
            table.HasCheckConstraint(
                "ck_ctdt_so_luong",
                "so_luong > 0");

            table.HasCheckConstraint(
                "ck_ctdt_khac",
                "ly_do <> 'KHAC' OR ghi_chu_ly_do IS NOT NULL");
        });

        builder.HasKey(x => new
        {
            x.PhieuDoiTraId,
            x.SanPhamId
        });

        builder.Property(x => x.PhieuDoiTraId)
            .HasColumnName("phieu_doi_tra_id")
            .HasColumnType("int unsigned")
            .IsRequired();

        builder.Property(x => x.SanPhamId)
            .HasColumnName("san_pham_id")
            .HasColumnType("int unsigned")
            .IsRequired();

        builder.Property(x => x.HoaDonId)
            .HasColumnName("hoa_don_id")
            .HasColumnType("int unsigned")
            .IsRequired();

        builder.Property(x => x.SoLuong)
            .HasColumnName("so_luong")
            .HasColumnType("int unsigned")
            .IsRequired();

        builder.Property(x => x.DonGiaThucTra)
            .HasColumnName("don_gia_thuc_tra")
            .HasColumnType("int unsigned")
            .IsRequired();

        builder.Property(x => x.LyDo)
            .HasColumnName("ly_do")
            .HasColumnType(
                "enum('LOI_SAN_PHAM','HET_HAN','KHONG_DUNG_NHU_CAU','KHAC')")
            .IsRequired();

        builder.Property(x => x.GhiChuLyDo)
            .HasColumnName("ghi_chu_ly_do")
            .HasColumnType("varchar(255)");

        builder.Property(x => x.TinhTrang)
            .HasColumnName("tinh_trang")
            .HasColumnType("varchar(255)");

        builder.Property(x => x.CachXuLy)
            .HasColumnName("cach_xu_ly")
            .HasColumnType("enum('NHAP_LAI','HUY')")
            .IsRequired();

        builder.Property(x => x.NgoaiLe)
            .HasColumnName("ngoai_le")
            .HasColumnType("tinyint(1)")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.GiaVon)
            .HasColumnName("gia_von")
            .HasColumnType("decimal(15,2)")
            .IsRequired();

        builder.Property(x => x.ThanhTien)
            .HasColumnName("thanh_tien")
            .HasColumnType("bigint unsigned")
            .HasComputedColumnSql(
                "so_luong * don_gia_thuc_tra",
                stored: true)
            .IsRequired();

        builder.Property(x => x.GiaTriHuy)
            .HasColumnName("gia_tri_huy")
            .HasColumnType("decimal(17,2)")
            .HasComputedColumnSql(
                "IF(cach_xu_ly = 'HUY', so_luong * gia_von, 0)",
                stored: true)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.HoaDonId,
            x.SanPhamId
        })
            .HasDatabaseName("idx_ctdt_dong_hd");

        builder.HasIndex(x => x.SanPhamId)
            .HasDatabaseName("idx_ctdt_san_pham");

        builder.HasOne(x => x.PhieuDoiTra)
            .WithMany()
            .HasForeignKey(x => x.PhieuDoiTraId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_ctdt_phieu");

        builder.HasOne(x => x.DongHoaDon)
            .WithMany()
            .HasForeignKey(x => new
            {
                x.HoaDonId,
                x.SanPhamId
            })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_ctdt_dong_hd");

        // The additional composite FK
        // (phieu_doi_tra_id, hoa_don_id)
        // -> phieu_doi_tra(id, hoa_don_goc_id)
        // is installed explicitly in the Migration.
        //
        // Do not create HasPrincipalKey here because the parent
        // table already has uq_pdt_id_hd_goc as a unique index.
    }
}