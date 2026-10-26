using ConvenienceStore.Web.Modules.Promotions.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConvenienceStore.Web.Modules.Promotions.Configurations;

public sealed class ChuongTrinhKhuyenMaiConfiguration
    : IEntityTypeConfiguration<ChuongTrinhKhuyenMai>
{
    public void Configure(
        EntityTypeBuilder<ChuongTrinhKhuyenMai> builder)
    {
        builder.ToTable("chuong_trinh_khuyen_mai", table =>
        {
            table.HasCheckConstraint(
                "ck_ctkm_thoi_gian",
                "ket_thuc_luc > bat_dau_luc");

            table.HasCheckConstraint(
                "ck_ctkm_gia_tri",
                "(loai_giam = 'PHAN_TRAM' " +
                "AND gia_tri BETWEEN 1 AND 100 " +
                "AND (giam_toi_da IS NULL " +
                "OR (giam_toi_da > 0 AND giam_toi_da % 100 = 0))) " +
                "OR (loai_giam = 'SO_TIEN' " +
                "AND gia_tri > 0 " +
                "AND gia_tri % 100 = 0 " +
                "AND giam_toi_da IS NULL)");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasColumnType("int unsigned")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.Ma)
            .HasColumnName("ma")
            .HasColumnType("varchar(20)")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Ten)
            .HasColumnName("ten")
            .HasColumnType("varchar(200)")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.MoTa)
            .HasColumnName("mo_ta")
            .HasColumnType("varchar(500)")
            .HasMaxLength(500);

        builder.Property(x => x.LoaiGiam)
            .HasColumnName("loai_giam")
            .HasColumnType("enum('PHAN_TRAM','SO_TIEN')")
            .IsRequired();

        builder.Property(x => x.GiaTri)
            .HasColumnName("gia_tri")
            .HasColumnType("int unsigned")
            .IsRequired();

        builder.Property(x => x.GiamToiDa)
            .HasColumnName("giam_toi_da")
            .HasColumnType("int unsigned");

        builder.Property(x => x.BatDauLuc)
            .HasColumnName("bat_dau_luc")
            .HasColumnType("datetime")
            .IsRequired();

        builder.Property(x => x.KetThucLuc)
            .HasColumnName("ket_thuc_luc")
            .HasColumnType("datetime")
            .IsRequired();

        builder.Property(x => x.ChiThanhVien)
            .HasColumnName("chi_thanh_vien")
            .HasColumnType("tinyint(1)")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.TrangThai)
            .HasColumnName("trang_thai")
            .HasColumnType("enum('NHAP','DA_KICH_HOAT','TAM_DUNG','DA_KET_THUC')")
            .HasDefaultValue("NHAP")
            .IsRequired();

        builder.Property(x => x.NguoiTaoId)
            .HasColumnName("nguoi_tao_id")
            .HasColumnType("int unsigned")
            .IsRequired();

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

        builder.HasIndex(x => x.Ma)
            .IsUnique()
            .HasDatabaseName("uq_ctkm_ma");

        builder.HasIndex(x => new
        {
            x.TrangThai,
            x.BatDauLuc,
            x.KetThucLuc
        })
        .HasDatabaseName("idx_ctkm_tt");

        builder.HasOne(x => x.NguoiTao)
            .WithMany()
            .HasForeignKey(x => x.NguoiTaoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_ctkm_nguoi_tao");
    }
}