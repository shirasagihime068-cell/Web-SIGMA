using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTK.SIGMA.Domain.Entities;

namespace PTK.SIGMA.Data.Context.Configurations;

public sealed class PeminjamanConfiguration : IEntityTypeConfiguration<Peminjaman>
{
    public void Configure(EntityTypeBuilder<Peminjaman> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.NamaPeminjam).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Jabatan).HasMaxLength(128);
        builder.Property(x => x.Fungsi).HasMaxLength(128);
        builder.Property(x => x.SerialNumber).HasMaxLength(256);
        builder.Property(x => x.Status).HasMaxLength(64);
        builder.Property(x => x.StatusPeminjaman).HasMaxLength(64);
        builder.Property(x => x.Keterangan).HasMaxLength(2000);
        builder.Property(x => x.NoBa).HasMaxLength(128);
        builder.HasIndex(x => x.TanggalPinjam);
        builder.HasIndex(x => new { x.Status, x.StatusPeminjaman });
        builder.HasOne(x => x.Perangkat)
            .WithMany()
            .HasForeignKey(x => x.PerangkatId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Lokasi)
            .WithMany()
            .HasForeignKey(x => x.LokasiId)
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}
