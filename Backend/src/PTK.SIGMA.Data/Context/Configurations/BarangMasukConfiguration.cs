using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTK.SIGMA.Domain.Entities;

namespace PTK.SIGMA.Data.Context.Configurations;

public sealed class BarangMasukConfiguration : IEntityTypeConfiguration<BarangMasuk>
{
    public void Configure(EntityTypeBuilder<BarangMasuk> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.Keterangan).HasMaxLength(2000);
        builder.HasIndex(x => x.TanggalMasuk);
        builder.HasIndex(x => x.Status);
        builder.HasOne(x => x.Perangkat)
            .WithMany()
            .HasForeignKey(x => x.PerangkatId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Vendor)
            .WithMany()
            .HasForeignKey(x => x.VendorId)
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}
