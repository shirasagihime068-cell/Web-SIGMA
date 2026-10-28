using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTK.SIGMA.Domain.Entities;

namespace PTK.SIGMA.Data.Context.Configurations;

public sealed class RakConfiguration : IEntityTypeConfiguration<Rak>
{
    public void Configure(EntityTypeBuilder<Rak> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.NamaRak).HasMaxLength(128).IsRequired();
    }
}
