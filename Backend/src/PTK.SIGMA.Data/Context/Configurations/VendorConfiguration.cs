using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTK.SIGMA.Domain.Entities;

namespace PTK.SIGMA.Data.Context.Configurations;

public sealed class VendorConfiguration : IEntityTypeConfiguration<Vendor>
{
    public void Configure(EntityTypeBuilder<Vendor> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.NamaVendor).HasMaxLength(256);
    }
}
