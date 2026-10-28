using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PTK.SIGMA.Data.Context;
using PTK.SIGMA.Domain.Entities;
using PTK.SIGMA.Domain.SigmaService; 
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Minio;
using Minio.DataModel.Args;

namespace PTK.SIGMA.Data.Api.SigmaApiRepo
{
    public class OpnameService : IOpnameService
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IMinioClient _minioClient;
        private readonly string _bucketName = "webgudang-storage";

        public OpnameService(AppDbContext context, INotificationService notificationService, IMinioClient minioClient)
        {
            _context = context;
            _notificationService = notificationService;
            _minioClient = minioClient;
        }

        public async Task<object> GetIndexDataAsync(string? searchString, string? lokasiRak, string? kategori, int? bulan, int? tahun)
        {
            int filterBulan = bulan ?? DateTime.Now.Month;
            int filterTahun = tahun ?? DateTime.Now.Year;

            // 1. Ambil list dropdown dengan AsNoTracking agar ringan
            var lokasiList = await _context.Raks
                .AsNoTracking()
                .Select(r => r.NamaRak)
                .OrderBy(r => r)
                .ToListAsync();

            var kategoriList = await _context.Kategoris
                .AsNoTracking()
                .Select(k => k.NamaKategori)
                .Distinct()
                .OrderBy(k => k)
                .ToListAsync();

            // 2. Siapkan Query Perangkat Dasar dengan AsNoTracking
            var query = _context.DataPerangkat
                .AsNoTracking()
                .Where(p => p.IsDeleted == false)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(p =>
                    (!string.IsNullOrEmpty(p.Nama) && p.Nama.Contains(searchString)) ||
                    (!string.IsNullOrEmpty(p.Kategori) && p.Kategori.Contains(searchString)) ||
                    (!string.IsNullOrEmpty(p.SnLaptop) && p.SnLaptop.Contains(searchString))
                );
            }

            if (!string.IsNullOrEmpty(lokasiRak))
            {
                query = query.Where(p => p.LokasiRak == lokasiRak);
            }

            if (!string.IsNullOrEmpty(kategori))
            {
                query = query.Where(p => p.Kategori == kategori);
            }

            // 3. PROYEKSI LANGSUNG KE DTO (Menghindari beban entitas berat & Include berlebih)
            var perangkatDtos = await query
                .OrderBy(p => p.LokasiRak)
                .ThenBy(p => p.Nama)
                .Select(p => new PerangkatResponseDto
                {
                    Id = p.Id,
                    Nama = p.Nama,
                    Kategori = p.Kategori,
                    SnLaptop = p.SnLaptop,
                    Jumlah = p.Jumlah,
                    LokasiId = p.LokasiId,
                    NamaLokasi = p.Lokasi != null ? p.Lokasi.NamaLokasi ?? "-" : "-",
                    VendorId = p.VendorId,
                    NamaVendor = p.Vendor != null ? p.Vendor.NamaVendor ?? "-" : "-",
                    Dokumentasi = p.Dokumentasi,
                    Keterangan = p.Keterangan,
                    IsVerified = p.IsVerified,
                    IsDeleted = p.IsDeleted,
                    CreatedAt = p.CreatedAt,
                    CreatedBy = p.CreatedBy,
                    UpdatedAt = p.UpdatedAt,
                    UpdatedBy = p.UpdatedBy
                })
                .ToListAsync();

            // Ambil ID perangkat yang tampil untuk membatasi pencarian Opname
            var perangkatIds = perangkatDtos.Select(p => p.Id).ToList();

            // Tentukan rentang tanggal (Sargable Optimization agar Index Database bekerja)
            var startDate = new DateTime(filterTahun, filterBulan, 1);
            var endDate = startDate.AddMonths(1);

            // 4. Query StockOpname menggunakan rentang tanggal & AsNoTracking
            var rawOpnames = await _context.StockOpnames
                .AsNoTracking()
                .Where(o => o.TanggalOpname >= startDate &&
                            o.TanggalOpname < endDate &&
                            perangkatIds.Contains(o.PerangkatId))
                .Select(o => new StockOpnameResponseDto
                {
                    Id = o.Id,
                    PerangkatId = o.PerangkatId,
                    NamaPerangkat = o.Perangkat != null ? o.Perangkat.Nama ?? "-" : "-",
                    JumlahSistem = o.JumlahSistem,
                    JumlahFisik = o.JumlahFisik,
                    Selisih = o.Selisih,
                    KondisiFisik = o.KondisiFisik,
                    Keterangan = o.Keterangan,
                    FotoOpname = o.FotoOpname,
                    TanggalOpname = o.TanggalOpname
                })
                .ToListAsync();

            // Grouping dipindah ke memori RAM aplikasi setelah data ditarik (jauh lebih cepat)
            var latestOpnames = rawOpnames
                .GroupBy(o => o.PerangkatId)
                .Select(g => g.OrderByDescending(o => o.TanggalOpname).First())
                .ToDictionary(o => o.PerangkatId, o => o);

            return new
            {
                filters = new
                {
                    currentFilter = searchString,
                    currentLokasi = lokasiRak,
                    currentKategori = kategori,
                    currentBulan = filterBulan,
                    currentTahun = filterTahun,
                    lokasiList,
                    kategoriList
                },
                perangkatList = perangkatDtos,
                latestOpnames
            };
        }

        public async Task<object> GetReminderStatusAsync()
        {
            var now = DateTime.Now;
            var startDate = new DateTime(now.Year, now.Month, 1);
            var endDate = startDate.AddMonths(1);

            var totalDevices = await _context.DataPerangkat
                .AsNoTracking()
                .CountAsync(p => !p.IsDeleted);

            var completedDevices = await (
                    from opname in _context.StockOpnames.AsNoTracking()
                    join perangkat in _context.DataPerangkat.AsNoTracking()
                        on opname.PerangkatId equals perangkat.Id
                    where !perangkat.IsDeleted
                          && opname.TanggalOpname >= startDate
                          && opname.TanggalOpname < endDate
                    select opname.PerangkatId)
                .Distinct()
                .CountAsync();

            var pendingDevices = Math.Max(totalDevices - completedDevices, 0);
            var periodLabel = now.ToString("MMMM yyyy",
                System.Globalization.CultureInfo.CurrentCulture);

            return new
            {
                periodKey = now.ToString("yyyy-MM"),
                bulan = now.Month,
                tahun = now.Year,
                periodLabel,
                totalDevices,
                completedDevices,
                pendingDevices,
                shouldRemind = totalDevices > 0 && pendingDevices > 0,
                isComplete = totalDevices == 0 || pendingDevices == 0
            };
        }

        public async Task<object> GetArsipDataAsync(int? bulan, int? tahun)
        {
            int targetBulan = bulan ?? DateTime.Now.Month;
            int targetTahun = tahun ?? DateTime.Now.Year;

            var startDate = new DateTime(targetTahun, targetBulan, 1);
            var endDate = startDate.AddMonths(1);

            var arsipData = await _context.StockOpnames
                .AsNoTracking()
                .Where(o => o.TanggalOpname >= startDate && o.TanggalOpname < endDate)
                .OrderByDescending(o => o.TanggalOpname)
                .Select(o => new StockOpnameResponseDto
                {
                    Id = o.Id,
                    PerangkatId = o.PerangkatId,
                    NamaPerangkat = o.Perangkat != null ? o.Perangkat.Nama : "-",
                    JumlahSistem = o.JumlahSistem,
                    JumlahFisik = o.JumlahFisik,
                    Selisih = o.Selisih,
                    KondisiFisik = o.KondisiFisik,
                    Keterangan = o.Keterangan,
                    FotoOpname = o.FotoOpname,
                    TanggalOpname = o.TanggalOpname
                })
                .ToListAsync();

            return new
            {
                bulan = targetBulan,
                tahun = targetTahun,
                data = arsipData
            };
        }

        // Method RecordOpname menggunakan StockOpnameCreateDto dan dilindungi try-catch
        public async Task<(bool Success, string Message)> RecordOpnameAsync(StockOpnameCreateDto dto)
        {
            var perangkat = await _context.DataPerangkat.FindAsync(dto.PerangkatId);
            if (perangkat == null) return (false, "Perangkat tidak ditemukan.");

            string? fotoPath = null;

            if (dto.FileFotoOpname != null && dto.FileFotoOpname.Length > 0)
            {
                var extension = Path.GetExtension(dto.FileFotoOpname.FileName).ToLowerInvariant();
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".heic" };
                if (!allowedExtensions.Contains(extension))
                    return (false, "Hanya file gambar JPG, JPEG, PNG, WEBP, atau HEIC yang diizinkan.");

                if (dto.FileFotoOpname.Length > 50 * 1024 * 1024)
                    return (false, "Ukuran foto opname maksimal 50MB.");

                try
                {
                    string fileName = Guid.NewGuid().ToString("N") + extension;
                    string objectName = "opname/" + fileName;

                    using (var stream = dto.FileFotoOpname.OpenReadStream())
                    {
                        var putObjectArgs = new PutObjectArgs()
                            .WithBucket(_bucketName)
                            .WithObject(objectName)
                            .WithStreamData(stream)
                            .WithObjectSize(stream.Length)
                            .WithContentType(extension switch
                            {
                                ".png" => "image/png",
                                ".webp" => "image/webp",
                                ".heic" => "image/heic",
                                _ => "image/jpeg"
                            });

                        await _minioClient.PutObjectAsync(putObjectArgs);
                    }

                    fotoPath = $"/api/files/opname/{fileName}";
                }
                catch (Exception ex)
                {
                    return (false, $"Gagal mengunggah foto ke MinIO: {ex.Message}");
                }
            }
            else
            {
                var opnameTerakhir = await _context.StockOpnames
                    .AsNoTracking()
                    .Where(o => o.PerangkatId == dto.PerangkatId && !string.IsNullOrEmpty(o.FotoOpname))
                    .OrderByDescending(o => o.Id)
                    .FirstOrDefaultAsync();

                fotoPath = opnameTerakhir != null ? opnameTerakhir.FotoOpname : perangkat.Dokumentasi;
            }

            var opname = new StockOpname
            {
                PerangkatId = dto.PerangkatId,
                JumlahSistem = perangkat.Jumlah,
                JumlahFisik = dto.JumlahFisik,
                KondisiFisik = dto.KondisiFisik,
                Keterangan = dto.Keterangan,
                FotoOpname = fotoPath,
                TanggalOpname = DateTime.Now
            };

            _context.StockOpnames.Add(opname);

            perangkat.Jumlah = dto.JumlahFisik;
            perangkat.Kondisi = dto.KondisiFisik;

            if (!string.IsNullOrEmpty(opname.FotoOpname))
            {
                perangkat.Dokumentasi = opname.FotoOpname;
            }

            _context.DataPerangkat.Update(perangkat);
            await _context.SaveChangesAsync();

            await _notificationService.KirimNotifikasiAsync(
                "Opname",
                "Stock Opname Diperbarui",
                $"Audit stok dicatat untuk perangkat: {perangkat.Nama}"
            );

            return (true, $"Stock opname untuk perangkat '{perangkat.Nama}' berhasil disimpan dan file terkirim ke MinIO!");
        }

        public async Task<bool> DeleteOpnameAsync(int id)
        {
            var opname = await _context.StockOpnames.FindAsync(id);
            if (opname == null) return false;

            var fileName = Path.GetFileName(opname.FotoOpname?.Replace('\\', '/'));

            _context.StockOpnames.Remove(opname);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(fileName) &&
                opname.FotoOpname?.Contains("/api/files/opname/", StringComparison.OrdinalIgnoreCase) == true)
            {
                try
                {
                    await _minioClient.RemoveObjectAsync(new RemoveObjectArgs()
                        .WithBucket(_bucketName)
                        .WithObject("opname/" + fileName));
                }
                catch
                {
                    // Penghapusan record tidak boleh gagal hanya karena objek
                    // lama sudah tidak ada pada storage.
                }
            }

            return true;
        }

        public async Task<byte[]> GeneratePdfReportAsync(string? lokasiRak, string? kategori, int? bulan, int? tahun)
        {
            int targetBulan = bulan ?? DateTime.Now.Month;
            int targetTahun = tahun ?? DateTime.Now.Year;

            var startDate = new DateTime(targetTahun, targetBulan, 1);
            var endDate = startDate.AddMonths(1);

            var query = _context.StockOpnames
                .AsNoTracking()
                .Include(o => o.Perangkat)
                .Where(o => o.TanggalOpname >= startDate && o.TanggalOpname < endDate)
                .AsQueryable();

            if (!string.IsNullOrEmpty(lokasiRak))
            {
                query = query.Where(o => o.Perangkat != null && o.Perangkat.LokasiRak == lokasiRak);
            }

            if (!string.IsNullOrEmpty(kategori))
            {
                query = query.Where(o => o.Perangkat != null && o.Perangkat.Kategori == kategori);
            }

            var data = await query
                .OrderBy(o => o.Perangkat != null ? o.Perangkat.LokasiRak : "")
                .ThenByDescending(o => o.TanggalOpname)
                .ToListAsync();

            string namaBulan = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(targetBulan);
            string periodeHeader = $"Periode: {namaBulan} {targetTahun}";
            if (!string.IsNullOrEmpty(lokasiRak)) periodeHeader += $" | Lokasi Rak: {lokasiRak}";
            if (!string.IsNullOrEmpty(kategori)) periodeHeader += $" | Kategori: {kategori}";

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(column =>
                        {
                            column.Item().Text("LAPORAN STOCK OPNAME GUDANG").FontSize(18).Bold().FontColor("#ED1B24");
                            column.Item().Text(periodeHeader).FontSize(10).FontColor(Colors.Grey.Darken2);
                        });
                    });

                    page.Content().PaddingVertical(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(35);
                            columns.ConstantColumn(75);
                            columns.RelativeColumn(1.2f);
                            columns.RelativeColumn(1.5f);
                            columns.ConstantColumn(40);
                            columns.ConstantColumn(40);
                            columns.ConstantColumn(40);
                            columns.RelativeColumn();
                            columns.RelativeColumn(1.5f);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text("No");
                            header.Cell().Element(CellStyle).Text("Tgl Opname");
                            header.Cell().Element(CellStyle).Text("Lokasi Rak");
                            header.Cell().Element(CellStyle).Text("Perangkat");
                            header.Cell().Element(CellStyle).Text("Sistem");
                            header.Cell().Element(CellStyle).Text("Fisik");
                            header.Cell().Element(CellStyle).Text("Selisih");
                            header.Cell().Element(CellStyle).Text("Kondisi");
                            header.Cell().Element(CellStyle).Text("Keterangan");

                            static IContainer CellStyle(IContainer container) => container
                                .DefaultTextStyle(x => x.SemiBold().FontSize(8).FontColor(Colors.White))
                                .PaddingVertical(6)
                                .Background("#003876")
                                .AlignCenter();
                        });

                        int index = 1;
                        foreach (var item in data)
                        {
                            table.Cell().Element(RowStyle).Text(index.ToString());
                            table.Cell().Element(RowStyle).Text(item.TanggalOpname.ToString("dd MMM yyyy"));
                            table.Cell().Element(RowStyle).Text(item.Perangkat != null ? (item.Perangkat.LokasiRak ?? "-") : "-");
                            table.Cell().Element(RowStyle).Text(item.Perangkat != null ? item.Perangkat.Nama : "-");
                            table.Cell().Element(RowStyle).Text(item.JumlahSistem.ToString());
                            table.Cell().Element(RowStyle).Text(item.JumlahFisik.ToString());
                            table.Cell().Element(RowStyle).Text(item.Selisih.ToString());
                            table.Cell().Element(RowStyle).Text(item.KondisiFisik ?? "-");
                            table.Cell().Element(RowStyle).Text(item.Keterangan ?? "-");

                            static IContainer RowStyle(IContainer container) => container
                                .DefaultTextStyle(x => x.FontSize(8))
                                .BorderBottom(1)
                                .BorderColor(Colors.Grey.Lighten2)
                                .PaddingVertical(5)
                                .AlignCenter();

                            index++;
                        }
                    });
                });
            });

            return document.GeneratePdf();
        }

        // Helper private method untuk memetakan StockOpname Entity ke StockOpnameResponseDto
        private static StockOpnameResponseDto MapToResponseDto(StockOpname o)
        {
            return new StockOpnameResponseDto
            {
                Id = o.Id,
                PerangkatId = o.PerangkatId,
                NamaPerangkat = o.Perangkat?.Nama ?? "-",
                JumlahSistem = o.JumlahSistem,
                JumlahFisik = o.JumlahFisik,
                Selisih = o.Selisih,
                KondisiFisik = o.KondisiFisik,
                Keterangan = o.Keterangan,
                FotoOpname = o.FotoOpname,
                TanggalOpname = o.TanggalOpname
            };
        }
    }
}
