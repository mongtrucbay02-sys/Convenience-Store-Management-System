using ConvenienceStore.Web.Modules.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConvenienceStore.Web.Modules.Identity.Configurations;

public sealed class VaiTroConfiguration : IEntityTypeConfiguration<VaiTro>
{
    public void Configure(EntityTypeBuilder<VaiTro> builder)
    {
        builder.ToTable("vai_tro", table =>
        {
            table.HasCheckConstraint(
                "ck_vai_tro_ma",
                "ma IN ('AD','TN','NK')");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasColumnType("tinyint unsigned")
            .ValueGeneratedNever();

        builder.Property(x => x.Ma)
            .HasColumnName("ma")
            .HasColumnType("char(2)")
            .IsRequired();

        builder.HasIndex(x => x.Ma)
            .IsUnique()
            .HasDatabaseName("uq_vai_tro_ma");

        builder.Property(x => x.Ten)
            .HasColumnName("ten")
            .HasColumnType("varchar(50)")
            .IsRequired();
    }
}
