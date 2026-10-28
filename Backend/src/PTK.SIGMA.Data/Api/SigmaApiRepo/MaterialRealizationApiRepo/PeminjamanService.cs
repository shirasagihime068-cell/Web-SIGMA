using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PTK.SIGMA.Data.Context;
using PTK.SIGMA.Domain.Entities;
using PTK.SIGMA.Domain.SigmaService;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Data;


namespace PTK.SIGMA.Data.Api.SigmaApiRepo
{
    public class PeminjamanService : IPeminjamanService
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;

        public PeminjamanService(AppDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<object> GetIndexDataAsync(string? searchString, DateTime? startDate, DateTime? endDate, string? kategori, int? page)
        {
            var kategoriList = await _context.Kategoris
                .Select(k => k.NamaKategori)
                .ToListAsync();

            var query = _context.Peminjamans
                .AsNoTracking()
                .Include(p => p.Perangkat)
                .Include(p => p.Lokasi)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(p =>
                    (!string.IsNullOrEmpty(p.NamaPeminjam) && p.NamaPeminjam.Contains(searchString)) ||
                    (p.Perangkat != null && !string.IsNullOrEmpty(p.Perangkat.Nama) && p.Perangkat.Nama.Contains(searchString))
                );
            }

            if (startDate.HasValue) query = query.Where(p => p.TanggalPinjam >= startDate.Value);
            if (endDate.HasValue) query = query.Where(p => p.TanggalPinjam < endDate.Value.Date.AddDays(1));
            if (!string.IsNullOrEmpty(kategori)) query = query.Where(p => p.Perangkat != null && p.Perangkat.Kategori == kategori);

            int pageSize = 15;
            int pageNumber = Math.Max(1, page ?? 1);
            int totalItems = await query.CountAsync();
            int totalPages = Math.Max(1, (int)Math.Ceiling(decimal.Divide(totalItems, pageSize)));
            pageNumber = Math.Min(pageNumber, totalPages);

            var items = await query
                .OrderByDescending(p => p.TanggalPinjam)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var itemDtos = items.Select(p => MapToResponseDto(p)).ToList();

            return new
            {
                kategoriList,
                pagination = new { currentPage = pageNumber, totalPages, totalItems },
                data = itemDtos
            };
        }

        public async Task<PeminjamanResponseDto?> GetByIdAsync(int id)
        {
            var p = await _context.Peminjamans
                .Include(x => x.Perangkat)
                .Include(x => x.Lokasi)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (p == null) return null;

            return MapToResponseDto(p);
        }

        public async Task<object> GetCreateOptionsAsync()
        {
            var perangkatList = await _context.DataPerangkat
                .Where(p => !p.IsDeleted)
                .Select(p => new
                {
                    p.Id,
                    p.Nama,
                    p.Jumlah,
                    p.Kategori,
                    p.SnLaptop
                })
                .ToListAsync();

            var kategoriList = await _context.Kategoris
                .Select(k => k.NamaKategori)
                .ToListAsync();

            var lokasiList = await _context.Lokasis
                .Select(l => new { l.Id, l.NamaLokasi })
                .ToListAsync();

            string nextNoBa = await GenerateNextNoBaAsync();

            return new
            {
                perangkatList,
                kategoriList,
                lokasiList,
                nextNoBa
            };
        }

        public async Task<byte[]> GeneratePdfReportAsync(DateTime? startDate, DateTime? endDate)
        {
            var query = _context.Peminjamans
                .Include(p => p.Perangkat)
                .Include(p => p.Lokasi)
                .AsQueryable();

            if (startDate.HasValue) query = query.Where(p => p.TanggalPinjam >= startDate.Value.Date);
            if (endDate.HasValue) query = query.Where(p => p.TanggalPinjam < endDate.Value.Date.AddDays(1));

            var data = await query.OrderByDescending(p => p.TanggalPinjam).ToListAsync();

            string periodeHeader = (startDate.HasValue && endDate.HasValue)
                ? $"Periode: {startDate.Value:dd MMM yyyy} - {endDate.Value:dd MMM yyyy}"
                : (startDate.HasValue ? $"Mulai Tanggal: {startDate.Value:dd MMM yyyy}" : (endDate.HasValue ? $"Sampai Tanggal: {endDate.Value:dd MMM yyyy}" : "Semua Periode"));

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4); // Menggunakan ukuran A4 Portrait
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(column =>
                        {
                            column.Item().Text("LAPORAN PEMINJAMAN BARANG").FontSize(16).Bold().FontColor("#ED1B24");
                            column.Item().Text(periodeHeader).FontSize(9).FontColor(Colors.Grey.Darken2);
                        });
                    });

                    page.Content().PaddingVertical(10).Table(table =>
                    {
                        // Menyesuaikan 9 kolom agar pas di ukuran A4 Portrait
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(70); // Tgl Pinjam
                            columns.ConstantColumn(70); // Tgl Kembali
                            columns.RelativeColumn(1.2f); // No. BA
                            columns.RelativeColumn(); // Peminjam
                            columns.RelativeColumn(); // Barang
                            columns.RelativeColumn(); // Lokasi
                            columns.ConstantColumn(25); // Qty
                            columns.RelativeColumn(); // Status
                            columns.RelativeColumn(); // Keterangan
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text("Tanggal Pinjam");
                            header.Cell().Element(CellStyle).Text("Tanggal Kembali");
                            header.Cell().Element(CellStyle).Text("No. BA");
                            header.Cell().Element(CellStyle).Text("Peminjam");
                            header.Cell().Element(CellStyle).Text("Barang");
                            header.Cell().Element(CellStyle).Text("Lokasi Barang");
                            header.Cell().Element(CellStyle).Text("Jumlah");
                            header.Cell().Element(CellStyle).Text("Status");
                            header.Cell().Element(CellStyle).Text("Keterangan");

                            static IContainer CellStyle(IContainer container) => container
                                .DefaultTextStyle(x => x.SemiBold().FontSize(7).FontColor(Colors.White))
                                .PaddingVertical(5).Background("#003876").AlignCenter();
                        });

                        foreach (var item in data)
                        {
                            string statusText = "-";
                            if (item.Status == "Pending" || string.IsNullOrEmpty(item.Status)) statusText = "Menunggu Verifikasi";
                            else if (item.Status == "Ditolak") statusText = "Ditolak";
                            else if (item.Status == "Disetujui")
                            {
                                statusText = (string.IsNullOrEmpty(item.StatusPeminjaman) || item.StatusPeminjaman == "Menunggu Verifikasi") ? "Dipinjam" : item.StatusPeminjaman;
                                if (statusText != "Dikembalikan" && item.TanggalKembali.HasValue && DateTime.Now.Date > item.TanggalKembali.Value.Date) statusText = "Terlambat";
                            }

                            table.Cell().Element(RowStyle).Text(item.TanggalPinjam.ToString("dd/MM/yy"));
                            table.Cell().Element(RowStyle).Text(item.TanggalKembali.HasValue ? item.TanggalKembali.Value.ToString("dd/MM/yy") : "-");
                            table.Cell().Element(RowStyle).Text(item.NoBa ?? "-");
                            table.Cell().Element(RowStyle).Text(item.NamaPeminjam ?? "-");
                            table.Cell().Element(RowStyle).Text(item.Perangkat != null ? item.Perangkat.Nama : "-");
                            table.Cell().Element(RowStyle).Text(item.Lokasi != null ? item.Lokasi.NamaLokasi : "-");
                            table.Cell().Element(RowStyle).Text(item.Jumlah.ToString());
                            table.Cell().Element(RowStyle).Text(statusText);
                            table.Cell().Element(RowStyle).Text(item.Keterangan ?? "-");

                            static IContainer RowStyle(IContainer container) => container
                                .DefaultTextStyle(x => x.FontSize(7)).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4).AlignCenter();
                        }
                    });
                });
            });

            return document.GeneratePdf();
        }

        public async Task<byte[]> GenerateExcelReportAsync(DateTime start, DateTime end)
        {
            var data = await _context.Peminjamans
                .Include(p => p.Perangkat)
                .Include(p => p.Lokasi)
                .Where(p => p.TanggalPinjam.Date >= start.Date && p.TanggalPinjam.Date <= end.Date)
                .OrderByDescending(p => p.TanggalPinjam)
                .ToListAsync();

            ExcelPackage.License.SetNonCommercialPersonal("PTK SIGMA");
            using var package = new ExcelPackage();
            var sheet = package.Workbook.Worksheets.Add("Laporan Peminjaman");

            sheet.Cells["A1"].Value = "LAPORAN PEMINJAMAN BARANG";
            sheet.Cells["A1"].Style.Font.Bold = true;
            sheet.Cells["A1"].Style.Font.Size = 14;

            sheet.Cells["A2"].Value = $"Periode: {start:dd MMM yyyy} - {end:dd MMM yyyy}";
            sheet.Cells["A2"].Style.Font.Italic = true;

            // 9 Kolom sesuai Index (A s.d. I)
            string[] headers = { "Tgl Pinjam", "Tgl Kembali", "No. BA", "Peminjam", "Barang", "Lokasi", "Qty", "Status", "Keterangan" };
            for (int i = 0; i < headers.Length; i++) sheet.Cells[4, i + 1].Value = headers[i];

            using (var range = sheet.Cells["A4:I4"])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(0, 56, 118));
                range.Style.Font.Color.SetColor(System.Drawing.Color.White);
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            int row = 5;
            foreach (var item in data)
            {
                string statusText = "-";
                if (item.Status == "Pending" || string.IsNullOrEmpty(item.Status)) statusText = "Menunggu Verifikasi";
                else if (item.Status == "Ditolak") statusText = "Ditolak";
                else if (item.Status == "Disetujui")
                {
                    statusText = (string.IsNullOrEmpty(item.StatusPeminjaman) || item.StatusPeminjaman == "Menunggu Verifikasi") ? "Dipinjam" : item.StatusPeminjaman;
                    if (statusText != "Dikembalikan" && item.TanggalKembali.HasValue && DateTime.Now.Date > item.TanggalKembali.Value.Date) statusText = "Terlambat";
                }

                sheet.Cells[row, 1].Value = item.TanggalPinjam.ToString("dd MMM yyyy");
                sheet.Cells[row, 2].Value = item.TanggalKembali.HasValue ? item.TanggalKembali.Value.ToString("dd MMM yyyy") : "-";
                sheet.Cells[row, 3].Value = item.NoBa ?? "-";
                sheet.Cells[row, 4].Value = item.NamaPeminjam ?? "-";
                sheet.Cells[row, 5].Value = item.Perangkat != null ? item.Perangkat.Nama : "-";
                sheet.Cells[row, 6].Value = item.Lokasi != null ? item.Lokasi.NamaLokasi : "-";
                sheet.Cells[row, 7].Value = item.Jumlah;
                sheet.Cells[row, 8].Value = statusText;
                sheet.Cells[row, 9].Value = item.Keterangan ?? "-";
                row++;
            }

            sheet.Cells.AutoFitColumns();
            return package.GetAsByteArray();
        }

        public async Task<string> GenerateNextNoBaAsync(DateTime? tanggalPinjam = null)
        {
            int tahun = tanggalPinjam.HasValue && tanggalPinjam != default ? tanggalPinjam.Value.Year : DateTime.Now.Year;
            int bulan = tanggalPinjam.HasValue && tanggalPinjam != default ? tanggalPinjam.Value.Month : DateTime.Now.Month;

            var listNoBa = await _context.Peminjamans
                .Where(p => p.TanggalPinjam.Year == tahun && p.TanggalPinjam.Month == bulan)
                .Select(p => p.NoBa)
                .ToListAsync();

            int maxUrut = 0;
            foreach (var noba in listNoBa)
            {
                if (!string.IsNullOrEmpty(noba))
                {
                    var parts = noba.Split('/');
                    if (parts.Length > 0 && int.TryParse(parts[0], out int val))
                    {
                        if (val > maxUrut) maxUrut = val;
                    }
                }
            }

            return $"{(maxUrut + 1):D2}/{bulan:D2}/BAST/ICTPTK/{tahun}";
        }

        public async Task<(bool Success, string Message, PeminjamanResponseDto? Data)> CreatePeminjamanAsync(PeminjamanCreateDto dto)
        {
            var peminjaman = new Peminjaman
            {
                NamaPeminjam = dto.NamaPeminjam,
                Jabatan = dto.Jabatan,
                Fungsi = dto.Fungsi,
                PerangkatId = dto.PerangkatId,
                SerialNumber = dto.SerialNumber,
                Jumlah = dto.Jumlah,
                LokasiId = dto.LokasiId,
                Keterangan = dto.Keterangan,
                Status = "Pending",
                StatusPeminjaman = "Menunggu Verifikasi",
                TanggalPinjam = dto.TanggalPinjam != default ? dto.TanggalPinjam : DateTime.Now,
                TanggalKembali = dto.TanggalKembali
            };

            await using (var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
            {
                peminjaman.NoBa = await GenerateNextNoBaAsync(peminjaman.TanggalPinjam);
                _context.Peminjamans.Add(peminjaman);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }

            await _notificationService.KirimNotifikasiAsync("Mengajukan", "Peminjaman Baru", $"Pengajuan peminjaman baru atas nama: {peminjaman.NamaPeminjam}");

            var responseDto = MapToResponseDto(peminjaman);
            return (true, $"Peminjaman berhasil dicatat dengan No. BA: {peminjaman.NoBa}", responseDto);
        }

        public async Task<(bool Success, string Message)> UpdatePeminjamanAsync(int id, PeminjamanCreateDto dto)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            var peminjaman = await _context.Peminjamans
                .Include(p => p.Perangkat)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (peminjaman == null) return (false, "Data peminjaman tidak ditemukan.");

            var isActiveLoan = peminjaman.Status == "Disetujui" &&
                (peminjaman.StatusPeminjaman == "Dipinjam" || peminjaman.StatusPeminjaman == "Dikeluarkan");

            if (isActiveLoan)
            {
                var oldDevice = peminjaman.Perangkat;
                var targetDevice = oldDevice?.Id == dto.PerangkatId
                    ? oldDevice
                    : await _context.DataPerangkat.FindAsync(dto.PerangkatId);

                if (oldDevice == null || targetDevice == null)
                    return (false, "Perangkat peminjaman tidak ditemukan.");

                oldDevice.Jumlah += peminjaman.Jumlah;
                if (targetDevice.Jumlah < dto.Jumlah)
                    return (false, "Stok perangkat tidak mencukupi untuk perubahan peminjaman.");

                targetDevice.Jumlah -= dto.Jumlah;
            }

            peminjaman.NamaPeminjam = dto.NamaPeminjam;
            peminjaman.Jabatan = dto.Jabatan;
            peminjaman.Fungsi = dto.Fungsi;
            peminjaman.PerangkatId = dto.PerangkatId;
            peminjaman.SerialNumber = dto.SerialNumber;
            peminjaman.Jumlah = dto.Jumlah;
            peminjaman.LokasiId = dto.LokasiId;
            peminjaman.Keterangan = dto.Keterangan;

            if (dto.TanggalPinjam != default)
            {
                peminjaman.TanggalPinjam = dto.TanggalPinjam;
            }
            peminjaman.TanggalKembali = dto.TanggalKembali;

            if (!string.IsNullOrEmpty(dto.NoBa))
            {
                peminjaman.NoBa = dto.NoBa;
            }

            _context.Peminjamans.Update(peminjaman);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return (true, "Data peminjaman berhasil diperbarui!");
        }

        public async Task<bool> SetujuiAsync(int id)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var peminjaman = await _context.Peminjamans.Include(p => p.Perangkat).FirstOrDefaultAsync(p => p.Id == id);
            if (peminjaman == null) return false;

            if (peminjaman.Status == "Disetujui") return true;
            if (peminjaman.Status != "Pending" || peminjaman.Perangkat == null ||
                peminjaman.Perangkat.Jumlah < peminjaman.Jumlah) return false;

            peminjaman.Status = "Disetujui";
            peminjaman.StatusPeminjaman = "Dipinjam";
            peminjaman.Perangkat.Jumlah -= peminjaman.Jumlah;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _notificationService.KirimNotifikasiAsync("Menyetujui", "Peminjaman", $"Menyetujui pengajuan peminjaman atas nama: {peminjaman.NamaPeminjam}");
            return true;
        }

        public async Task<bool> TolakAsync(int id)
        {
            var peminjaman = await _context.Peminjamans.FirstOrDefaultAsync(p => p.Id == id);
            if (peminjaman == null) return false;

            if (peminjaman.Status == "Ditolak") return true;
            if (peminjaman.Status != "Pending") return false;

            peminjaman.Status = "Ditolak";
            peminjaman.StatusPeminjaman = "Menunggu Verifikasi";
            await _context.SaveChangesAsync();

            await _notificationService.KirimNotifikasiAsync("Menolak", "Peminjaman", $"Menolak pengajuan peminjaman atas nama: {peminjaman.NamaPeminjam}");
            return true;
        }

        public async Task<bool> KembalikanAsync(int id)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var peminjaman = await _context.Peminjamans.Include(p => p.Perangkat).FirstOrDefaultAsync(p => p.Id == id);
            if (peminjaman == null) return false;

            if (peminjaman.StatusPeminjaman == "Dikembalikan") return true;
            if (peminjaman.Status != "Disetujui" || peminjaman.Perangkat == null ||
                (peminjaman.StatusPeminjaman != "Dipinjam" && peminjaman.StatusPeminjaman != "Dikeluarkan"))
                return false;

            peminjaman.Perangkat.Jumlah += peminjaman.Jumlah;
            peminjaman.StatusPeminjaman = "Dikembalikan";

            if (!peminjaman.TanggalKembali.HasValue) peminjaman.TanggalKembali = DateTime.Now;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _notificationService.KirimNotifikasiAsync("Mengubah", "Peminjaman", $"Perangkat yang dipinjam oleh {peminjaman.NamaPeminjam} telah dikembalikan.");
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var peminjaman = await _context.Peminjamans.Include(p => p.Perangkat).FirstOrDefaultAsync(p => p.Id == id);
            if (peminjaman == null) return false;

            if (peminjaman.Perangkat != null && peminjaman.Status == "Disetujui" &&
                (peminjaman.StatusPeminjaman == "Dipinjam" || peminjaman.StatusPeminjaman == "Dikeluarkan"))
            {
                peminjaman.Perangkat.Jumlah += peminjaman.Jumlah;
                _context.Update(peminjaman.Perangkat);
            }

            _context.Peminjamans.Remove(peminjaman);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _notificationService.KirimNotifikasiAsync("Menghapus", "Peminjaman", $"Menghapus data peminjaman atas nama: {peminjaman.NamaPeminjam}");
            return true;
        }

        private static PeminjamanResponseDto MapToResponseDto(Peminjaman p)
        {
            return new PeminjamanResponseDto
            {
                Id = p.Id,
                NamaPeminjam = p.NamaPeminjam,
                Jabatan = p.Jabatan,
                Fungsi = p.Fungsi,
                PerangkatId = p.PerangkatId,
                NamaPerangkat = p.Perangkat?.Nama ?? "-",
                SerialNumber = p.SerialNumber,
                Jumlah = p.Jumlah,
                LokasiId = p.LokasiId,
                NamaLokasi = p.Lokasi?.NamaLokasi ?? "-",
                TanggalPinjam = p.TanggalPinjam,
                TanggalKembali = p.TanggalKembali,
                Status = p.Status,
                StatusPeminjaman = p.StatusPeminjaman,
                Keterangan = p.Keterangan,
                NoBa = p.NoBa
            };
        }
    }
}
