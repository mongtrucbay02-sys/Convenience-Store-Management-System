using ConvenienceStore.Web.Modules.Returns.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConvenienceStore.Web.Modules.Returns.Configurations;

public sealed class ChinhSachDoiTraConfiguration
    : IEntityTypeConfiguration<ChinhSachDoiTra>
{
    public void Configure(EntityTypeBuilder<ChinhSachDoiTra> builder)
    {
        builder.ToTable("chinh_sach_doi_tra");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasColumnType("int unsigned")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.SoNgayDoiTra)
            .HasColumnName("so_ngay_doi_tra")
            .HasColumnType("smallint unsigned")
            .HasDefaultValue((ushort)7)
            .IsRequired();

        builder.Property(x => x.NguongTienCanDuyet)
            .HasColumnName("nguong_tien_can_duyet")
            .HasColumnType("bigint unsigned")
            .HasDefaultValue(500000UL)
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
            .HasDatabaseName("idx_csdt_hieu_luc");

        builder.HasOne(x => x.NguoiTao)
            .WithMany()
            .HasForeignKey(x => x.NguoiTaoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_csdt_nguoi_tao");
    }
}