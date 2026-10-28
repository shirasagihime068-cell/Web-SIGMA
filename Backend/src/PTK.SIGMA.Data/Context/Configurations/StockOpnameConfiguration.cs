using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTK.SIGMA.Domain.Entities;

namespace PTK.SIGMA.Data.Context.Configurations;

public sealed class StockOpnameConfiguration : IEntityTypeConfiguration<StockOpname>
{
    public void Configure(EntityTypeBuilder<StockOpname> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.KondisiFisik).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Keterangan).HasMaxLength(2000);
        builder.Property(x => x.FotoOpname).HasMaxLength(1000);
        builder.HasIndex(x => x.TanggalOpname);
        builder.HasIndex(x => new { x.PerangkatId, x.TanggalOpname });
        builder.HasOne(x => x.Perangkat)
            .WithMany()
            .HasForeignKey(x => x.PerangkatId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
