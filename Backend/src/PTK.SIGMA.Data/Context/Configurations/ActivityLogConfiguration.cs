using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTK.SIGMA.Domain.Entities;

namespace PTK.SIGMA.Data.Context.Configurations;

public sealed class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Username).HasMaxLength(128);
        builder.Property(x => x.Aksi).HasMaxLength(128);
        builder.Property(x => x.Target).HasMaxLength(256);
        builder.Property(x => x.Detail).HasMaxLength(2000);
        builder.Property(x => x.Waktu).IsRequired();
        builder.HasIndex(x => new { x.IsRead, x.Waktu });
    }
}
