using ConvenienceStore.Web.Modules.Shared.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConvenienceStore.Web.Modules.Shared.Configurations;

public sealed class BoDemChungTuConfiguration
    : IEntityTypeConfiguration<BoDemChungTu>
{
    public void Configure(EntityTypeBuilder<BoDemChungTu> builder)
    {
        builder.ToTable("bo_dem_chung_tu", table =>
        {
            table.HasCheckConstraint(
                "ck_bo_dem_tien_to",
                "tien_to IN ('PN','HD','TH','KK','XH','SP','NCC','KM')");
        });

        builder.HasKey(x => new { x.TienTo, x.Ky });

        builder.Property(x => x.TienTo)
            .HasColumnName("tien_to")
            .HasColumnType("varchar(3)")
            .IsRequired();

        builder.Property(x => x.Ky)
            .HasColumnName("ky")
            .HasColumnType("char(8)")
            .HasDefaultValue("")
            .IsRequired();

        builder.Property(x => x.SoCuoi)
            .HasColumnName("so_cuoi")
            .HasColumnType("int unsigned")
            .HasDefaultValue(0u)
            .IsRequired();
    }
}
