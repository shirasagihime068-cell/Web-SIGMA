using Microsoft.EntityFrameworkCore;
using PTK.SIGMA.Domain.Entities;

namespace PTK.SIGMA.Data.Context;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Perangkat> DataPerangkat => Set<Perangkat>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<Peminjaman> Peminjamans => Set<Peminjaman>();
    public DbSet<User> Users => Set<User>();
    public DbSet<BarangMasuk> BarangMasuks => Set<BarangMasuk>();
    public DbSet<BarangKeluar> BarangKeluars => Set<BarangKeluar>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<Kategori> Kategoris => Set<Kategori>();
    public DbSet<StockOpname> StockOpnames => Set<StockOpname>();
    public DbSet<Rak> Raks => Set<Rak>();
    public DbSet<Lokasi> Lokasis => Set<Lokasi>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
