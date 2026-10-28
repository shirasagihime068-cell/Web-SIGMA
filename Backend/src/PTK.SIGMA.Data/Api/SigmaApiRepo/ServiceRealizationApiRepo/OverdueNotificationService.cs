using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTK.SIGMA.Data.Context;

using PTK.SIGMA.Domain.SigmaService;

namespace PTK.SIGMA.Data.Api.SigmaApiRepo
{
    public class OverdueNotificationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OverdueNotificationService> _logger;

        public OverdueNotificationService(IServiceProvider serviceProvider, ILogger<OverdueNotificationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OverdueNotificationService berhasil dimulai.");

            // Jeda awal sebentar agar aplikasi selesai melakukan startup penuh
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckOverdueItemsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Terjadi kesalahan saat memeriksa peminjaman yang melewati batas waktu.");
                }

                // Interval pemeriksaan otomatis (ubah kembali ke TimeSpan.FromHours(24) jika sudah selesai testing)
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }

        private async Task CheckOverdueItemsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            var today = DateTime.Now.Date;

            // Cari data peminjaman yang statusnya "Dipinjam" dan tanggal kembalinya sudah lewat
            var overdueItems = await context.Peminjamans
                .Include(p => p.Perangkat)
                .Where(p => p.StatusPeminjaman == "Dipinjam"
                         && p.TanggalKembali.HasValue
                         && p.TanggalKembali.Value.Date < today)
                .ToListAsync();

            if (overdueItems.Any())
            {
                foreach (var item in overdueItems)
                {
                    string namaBarang = item.Perangkat?.Nama ?? "Perangkat";
                    string namaPeminjam = item.NamaPeminjam ?? "Peminjam";
                    string tglKembali = item.TanggalKembali?.ToString("dd MMM yyyy") ?? "-";

                    string detailPesan = $"Peringatan! Perangkat '{namaBarang}' yang dipinjam oleh {namaPeminjam} telah melewati batas waktu pengembalian ({tglKembali}).";

                    // === CEK APAKAH NOTIFIKASI UNTUK PEMINJAMAN INI SUDAH DIKIRIM HARI INI ===
                    bool sudahDikiriminHariIni = await context.ActivityLogs.AnyAsync(l =>
                        l.Aksi == "Terlambat" &&
                        l.Detail == detailPesan &&
                        l.Waktu.Date == today
                    );

                    // Jika hari ini BELUM pernah dikirim, baru kirim notifikasinya
                    if (!sudahDikiriminHariIni)
                    {
                        await notificationService.KirimNotifikasiAsync(
                            "Terlambat",
                            "Peminjaman Overdue",
                            detailPesan
                        );
                    }
                }
            }
        }
    }
}
