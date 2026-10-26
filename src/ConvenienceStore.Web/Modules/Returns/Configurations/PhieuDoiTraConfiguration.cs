using ConvenienceStore.Web.Modules.Returns.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConvenienceStore.Web.Modules.Returns.Configurations;

public sealed class PhieuDoiTraConfiguration
    : IEntityTypeConfiguration<PhieuDoiTra>
{
    public void Configure(EntityTypeBuilder<PhieuDoiTra> builder)
    {
        builder.ToTable("phieu_doi_tra", table =>
        {
            table.HasCheckConstraint(
                "ck_pdt_tra",
                "loai <> 'TRA_HANG' OR " +
                "(hoa_don_moi_id IS NULL AND tien_thu_them = 0 " +
                "AND tien_hoan = gia_tri_hang_tra)");

            table.HasCheckConstraint(
                "ck_pdt_doi",
                "loai <> 'DOI_HANG' OR " +
                "(tien_hoan = 0 AND " +
                "(trang_thai <> 'HOAN_TAT' OR hoa_don_moi_id IS NOT NULL))");

            table.HasCheckConstraint(
                "ck_pdt_hoan_tat",
                "trang_thai <> 'HOAN_TAT' OR hoan_tat_luc IS NOT NULL");

            table.HasCheckConstraint(
                "ck_pdt_khong_can_duyet",
                "can_duyet_qua_han OR can_duyet_danh_muc " +
                "OR can_duyet_vuot_nguong OR " +
                "(trang_thai = 'HOAN_TAT' AND nguoi_duyet_id IS NULL)");

            table.HasCheckConstraint(
                "ck_pdt_can_duyet",
                "NOT (can_duyet_qua_han OR can_duyet_danh_muc " +
                "OR can_duyet_vuot_nguong) " +
                "OR trang_thai = 'CHO_DUYET' " +
                "OR (nguoi_duyet_id IS NOT NULL AND duyet_luc IS NOT NULL)");

            table.HasCheckConstraint(
                "ck_pdt_da_duyet",
                "trang_thai <> 'DA_DUYET' OR loai = 'DOI_HANG'");
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

        builder.Property(x => x.Loai)
            .HasColumnName("loai")
            .HasColumnType("enum('TRA_HANG','DOI_HANG')")
            .IsRequired();

        builder.Property(x => x.HoaDonGocId)
            .HasColumnName("hoa_don_goc_id")
            .HasColumnType("int unsigned")
            .IsRequired();

        builder.Property(x => x.HoaDonMoiId)
            .HasColumnName("hoa_don_moi_id")
            .HasColumnType("int unsigned");

        builder.Property(x => x.KhachHangId)
            .HasColumnName("khach_hang_id")
            .HasColumnType("int unsigned");

        builder.Property(x => x.ChinhSachId)
            .HasColumnName("chinh_sach_id")
            .HasColumnType("int unsigned")
            .IsRequired();

        builder.Property(x => x.TrangThai)
            .HasColumnName("trang_thai")
            .HasColumnType("enum('CHO_DUYET','DA_DUYET','HOAN_TAT','BI_TU_CHOI')")
            .IsRequired();

        builder.Property(x => x.CanDuyetQuaHan)
            .HasColumnName("can_duyet_qua_han")
            .HasColumnType("tinyint(1)")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.CanDuyetDanhMuc)
            .HasColumnName("can_duyet_danh_muc")
            .HasColumnType("tinyint(1)")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.CanDuyetVuotNguong)
            .HasColumnName("can_duyet_vuot_nguong")
            .HasColumnType("tinyint(1)")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.GiaTriHangTra)
            .HasColumnName("gia_tri_hang_tra")
            .HasColumnType("bigint unsigned")
            .IsRequired();

        builder.Property(x => x.TienHoan)
            .HasColumnName("tien_hoan")
            .HasColumnType("bigint unsigned")
            .HasDefaultValue(0UL)
            .IsRequired();

        builder.Property(x => x.TienThuThem)
            .HasColumnName("tien_thu_them")
            .HasColumnType("bigint unsigned")
            .HasDefaultValue(0UL)
            .IsRequired();

        builder.Property(x => x.DiemThuHoi)
            .HasColumnName("diem_thu_hoi")
            .HasColumnType("int unsigned")
            .HasDefaultValue(0U)
            .IsRequired();

        builder.Property(x => x.ThuNganId)
            .HasColumnName("thu_ngan_id")
            .HasColumnType("int unsigned")
            .IsRequired();

        builder.Property(x => x.NgayLap)
            .HasColumnName("ngay_lap")
            .HasColumnType("datetime")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(x => x.NguoiDuyetId)
            .HasColumnName("nguoi_duyet_id")
            .HasColumnType("int unsigned");

        builder.Property(x => x.DuyetLuc)
            .HasColumnName("duyet_luc")
            .HasColumnType("datetime");

        builder.Property(x => x.GhiChuDuyet)
            .HasColumnName("ghi_chu_duyet")
            .HasColumnType("varchar(255)");

        builder.Property(x => x.HoanTatLuc)
            .HasColumnName("hoan_tat_luc")
            .HasColumnType("datetime");

        builder.HasIndex(x => x.MaPhieu)
            .IsUnique()
            .HasDatabaseName("uq_pdt_ma");

        builder.HasIndex(x => x.HoaDonMoiId)
            .IsUnique()
            .HasDatabaseName("uq_pdt_hd_moi");

        builder.HasIndex(x => new { x.Id, x.HoaDonGocId })
            .IsUnique()
            .HasDatabaseName("uq_pdt_id_hd_goc");

        builder.HasIndex(x => x.HoaDonGocId)
            .HasDatabaseName("idx_pdt_hd_goc");

        builder.HasIndex(x => new { x.TrangThai, x.HoanTatLuc })
            .HasDatabaseName("idx_pdt_tt");

        builder.HasIndex(x => new { x.ThuNganId, x.NgayLap })
            .HasDatabaseName("idx_pdt_thu_ngan");

        builder.HasOne(x => x.HoaDonGoc)
            .WithMany()
            .HasForeignKey(x => x.HoaDonGocId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_pdt_hd_goc");

        builder.HasOne(x => x.HoaDonMoi)
            .WithMany()
            .HasForeignKey(x => x.HoaDonMoiId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_pdt_hd_moi");

        builder.HasOne(x => x.KhachHang)
            .WithMany()
            .HasForeignKey(x => x.KhachHangId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_pdt_khach");

        builder.HasOne(x => x.ChinhSach)
            .WithMany()
            .HasForeignKey(x => x.ChinhSachId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_pdt_cs");

        builder.HasOne(x => x.ThuNgan)
            .WithMany()
            .HasForeignKey(x => x.ThuNganId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_pdt_thu_ngan");

        builder.HasOne(x => x.NguoiDuyet)
            .WithMany()
            .HasForeignKey(x => x.NguoiDuyetId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_pdt_nguoi_duyet");
    }
}