using ConvenienceStore.Web.Modules.Customers.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConvenienceStore.Web.Modules.Customers.Configurations;

public sealed class QuyTacTichDiemConfiguration
    : IEntityTypeConfiguration<QuyTacTichDiem>
{
    public void Configure(EntityTypeBuilder<QuyTacTichDiem> builder)
    {
        builder.ToTable("quy_tac_tich_diem", table =>
        {
            table.HasCheckConstraint(
                "ck_qttd_so_tien",
                "so_tien_mot_diem > 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasColumnType("int unsigned")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.SoTienMotDiem)
            .HasColumnName("so_tien_mot_diem")
            .HasColumnType("int unsigned")
            .HasDefaultValue(10000u)
            .IsRequired();

        builder.Property(x => x.DangBat)
            .HasColumnName("dang_bat")
            .HasColumnType("tinyint(1)")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.HieuLucTu)
            .HasColumnName("hieu_luc_tu")
            .HasColumnType("datetime")
            .IsRequired();

        builder.Property(x => x.NguoiTaoId)
            .HasColumnName("nguoi_tao_id")
            .HasColumnType("int unsigned");

        builder.Property(x => x.NgayTao)
            .HasColumnName("ngay_tao")
            .HasColumnType("datetime")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.HasIndex(x => x.HieuLucTu)
            .HasDatabaseName("idx_qttd_hieu_luc");

        builder.HasOne(x => x.NguoiTao)
            .WithMany()
            .HasForeignKey(x => x.NguoiTaoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_qttd_nguoi_tao");
    }
}