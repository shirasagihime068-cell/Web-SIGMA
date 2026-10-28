using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTK.SIGMA.Domain.Entities;

namespace PTK.SIGMA.Data.Context.Configurations;

public sealed class PerangkatConfiguration : IEntityTypeConfiguration<Perangkat>
{
    public void Configure(EntityTypeBuilder<Perangkat> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Nama).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Kategori).HasMaxLength(128).IsRequired();
        builder.Property(x => x.TypeLaptop).HasMaxLength(128);
        builder.Property(x => x.ProductId).HasMaxLength(128);
        builder.Property(x => x.SnLaptop).HasMaxLength(256);
        builder.Property(x => x.StatusSewa).HasMaxLength(64);
        builder.Property(x => x.Keterangan).HasMaxLength(2000);
        builder.Property(x => x.Kondisi).HasMaxLength(64);
        builder.Property(x => x.Dokumentasi).HasMaxLength(1000);
        builder.Property(x => x.LokasiRak).HasMaxLength(128);
        builder.Property(x => x.LokasiBarang).HasMaxLength(256);
        builder.Property(x => x.CreatedBy).HasMaxLength(256);
        builder.Property(x => x.UpdatedBy).HasMaxLength(256);

        builder.HasIndex(x => new { x.IsDeleted, x.IsVerified, x.CreatedAt });
        builder.HasIndex(x => new { x.IsDeleted, x.IsVerified, x.Kategori });
        builder.HasIndex(x => new { x.IsDeleted, x.IsVerified, x.Jumlah });
        builder.HasIndex(x => x.Nama);
        builder.HasIndex(x => x.SnLaptop);
        builder.HasOne(x => x.Vendor)
            .WithMany()
            .HasForeignKey(x => x.VendorId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.Lokasi)
            .WithMany(x => x.Perangkats)
            .HasForeignKey(x => x.LokasiId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
