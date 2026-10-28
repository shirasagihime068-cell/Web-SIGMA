using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTK.SIGMA.Domain.Entities;

namespace PTK.SIGMA.Data.Context.Configurations;

public sealed class LokasiConfiguration : IEntityTypeConfiguration<Lokasi>
{
    public void Configure(EntityTypeBuilder<Lokasi> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.NamaLokasi).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Keterangan).HasMaxLength(1000);
    }
}
