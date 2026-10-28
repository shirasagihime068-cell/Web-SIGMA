using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTK.SIGMA.Domain.Entities;

namespace PTK.SIGMA.Data.Context.Configurations;

public sealed class BarangKeluarConfiguration : IEntityTypeConfiguration<BarangKeluar>
{
    public void Configure(EntityTypeBuilder<BarangKeluar> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.Penerima).HasMaxLength(256);
        builder.Property(x => x.Keterangan).HasMaxLength(2000);
        builder.HasIndex(x => x.TanggalKeluar);
        builder.HasIndex(x => x.Status);
        builder.HasOne(x => x.Perangkat)
            .WithMany()
            .HasForeignKey(x => x.PerangkatId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
