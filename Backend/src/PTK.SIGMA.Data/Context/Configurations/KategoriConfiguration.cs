using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTK.SIGMA.Domain.Entities;

namespace PTK.SIGMA.Data.Context.Configurations;

public sealed class KategoriConfiguration : IEntityTypeConfiguration<Kategori>
{
    public void Configure(EntityTypeBuilder<Kategori> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.NamaKategori).HasMaxLength(128).IsRequired();
        builder.HasData(
            new Kategori { Id = 1, NamaKategori = "Laptop SM5" },
            new Kategori { Id = 2, NamaKategori = "Laptop Non SM5" },
            new Kategori { Id = 3, NamaKategori = "Tinta / Toner / Cartridge" },
            new Kategori { Id = 4, NamaKategori = "Perangkat ICT Operation" });
    }
}
