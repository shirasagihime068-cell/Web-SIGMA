using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PTK.SIGMA.Data.Context;
using PTK.SIGMA.Domain.Entities;
using PTK.SIGMA.Utilities.Enums;
using PTK.SIGMA.Domain.SigmaService;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using OfficeOpenXml;
using OfficeOpenXml.Style;


namespace PTK.SIGMA.Data.Api.SigmaApiRepo
{
    public class KelolaBarangService : IKelolaBarangService
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;

        public KelolaBarangService(AppDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<List<TransaksiHistoryItemResponseDto>> GetIndexTransactionsAsync()
        {
#pragma warning disable CS8602
            // Ambil data mentah ke memori terlebih dahulu untuk menghindari error terjemahan Union di EF Core
            var dataMasuk = await _context.BarangMasuks
                .Include(b => b.Perangkat)
                    .ThenInclude(p => p.Vendor)
                .ToListAsync();

            var dataKeluar = await _context.BarangKeluars
                .Include(b => b.Perangkat)
                    .ThenInclude(p => p.Vendor)
                .ToListAsync();

            var listMasuk = dataMasuk.Select(b => new TransaksiHistoryItemResponseDto
            {
                Id = b.Id,
                Status = b.Status.ToString(),
                Tanggal = b.TanggalMasuk,
                NamaBarang = b.Perangkat != null ? b.Perangkat.Nama : "Barang Dihapus",
                Jenis = "Masuk",
                Jumlah = b.Jumlah,
                Vendor = (b.Perangkat != null && b.Perangkat.Vendor != null) ? b.Perangkat.Vendor.NamaVendor : "-",
                Penerima = "-",
                Keterangan = b.Keterangan
            });

            var listKeluar = dataKeluar.Select(b => new TransaksiHistoryItemResponseDto
            {
                Id = b.Id,
                Status = b.Status.ToString(),
                Tanggal = b.TanggalKeluar,
                NamaBarang = b.Perangkat != null ? b.Perangkat.Nama : "Barang Dihapus",
                Jenis = "Keluar",
                Jumlah = b.Jumlah,
                Vendor = (b.Perangkat != null && b.Perangkat.Vendor != null) ? b.Perangkat.Vendor.NamaVendor : "-",
                Penerima = b.Penerima ?? "-",
                Keterangan = b.Keterangan
            });
#pragma warning restore CS8602

            return listMasuk.Concat(listKeluar)
                .OrderByDescending(x => x.Tanggal)
                .ToList();
        }

        // Kategori Logic
        public async Task<List<Kategori>> GetKategorisAsync() => await _context.Kategoris.ToListAsync();
        public async Task<Kategori?> GetKategoriByIdAsync(int id) => await _context.Kategoris.FindAsync(id);

        public async Task AddKategoriAsync(Kategori model)
        {
            _context.Kategoris.Add(model);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateKategoriAsync(Kategori model)
        {
            _context.Kategoris.Update(model);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteKategoriAsync(Kategori kategori)
        {
            _context.Kategoris.Remove(kategori);
            await _context.SaveChangesAsync();
        }

        // Lokasi Master Logic
        public async Task<List<Lokasi>> GetLokasisAsync() => await _context.Lokasis.OrderBy(l => l.NamaLokasi).ToListAsync();

        public async Task<(bool Success, string Message)> TambahLokasiAsync(string namaLokasi)
        {
            if (string.IsNullOrWhiteSpace(namaLokasi)) return (false, "Nama lokasi tidak boleh kosong.");

            string cleanName = namaLokasi.Trim();
            bool exists = await _context.Lokasis.AnyAsync(l => l.NamaLokasi != null && l.NamaLokasi.ToLower() == cleanName.ToLower());
            if (exists) return (false, $"Lokasi '{cleanName}' sudah terdaftar di sistem.");

            _context.Lokasis.Add(new Lokasi { NamaLokasi = cleanName });
            await _context.SaveChangesAsync();
            return (true, $"Lokasi '{cleanName}' berhasil ditambahkan!");
        }

        public async Task HapusLokasiAsync(Lokasi lokasi)
        {
            _context.Lokasis.Remove(lokasi);
            await _context.SaveChangesAsync();
        }

        // Rak Logic
        public async Task<List<Rak>> GetRaksAsync() => await _context.Raks.OrderBy(r => r.NamaRak).ToListAsync();

        public async Task<(bool Success, string Message)> TambahRakAsync(string namaRak)
        {
            if (string.IsNullOrWhiteSpace(namaRak)) return (false, "Nama rak tidak boleh kosong.");

            string cleanName = namaRak.Trim();
            bool exists = await _context.Raks.AnyAsync(r => r.NamaRak.ToLower() == cleanName.ToLower());
            if (exists) return (false, $"Lokasi rak '{cleanName}' sudah terdaftar di sistem.");

            _context.Raks.Add(new Rak { NamaRak = cleanName });
            await _context.SaveChangesAsync();
            return (true, $"Lokasi rak '{cleanName}' berhasil ditambahkan!");
        }

        public async Task HapusRakAsync(Rak rak)
        {
            _context.Raks.Remove(rak);
            await _context.SaveChangesAsync();
        }

        // Approval / Transaksi Logic
        public async Task<bool> ApproveMasukAsync(int id)
        {
            var transaksi = await _context.BarangMasuks.FindAsync(id);
            if (transaksi == null || transaksi.Status != StatusTransaksi.Menunggu) return false;

            transaksi.Status = StatusTransaksi.Disetujui;
            var perangkat = await _context.DataPerangkat.FindAsync(transaksi.PerangkatId);
            if (perangkat != null) perangkat.Jumlah += transaksi.Jumlah;

            await _context.SaveChangesAsync();
            await _notificationService.KirimNotifikasiAsync("Menyetujui", "Barang Masuk", $"Menyetujui barang masuk ID {id} sebanyak {transaksi.Jumlah} unit");
            return true;
        }

        public async Task<bool> TolakMasukAsync(int id)
        {
            var transaksi = await _context.BarangMasuks.FindAsync(id);
            if (transaksi == null || transaksi.Status != StatusTransaksi.Menunggu) return false;

            transaksi.Status = StatusTransaksi.Ditolak;
            await _context.SaveChangesAsync();
            await _notificationService.KirimNotifikasiAsync("Menolak", "Barang Masuk", $"Menolak pengajuan barang masuk ID {id} sebanyak {transaksi.Jumlah} unit");
            return true;
        }

        public async Task<(bool Success, string Message)> ApproveKeluarAsync(int id)
        {
            var transaksi = await _context.BarangKeluars.FindAsync(id);
            if (transaksi == null || transaksi.Status != StatusTransaksi.Menunggu) return (false, "Transaksi tidak ditemukan.");

            var perangkat = await _context.DataPerangkat.FindAsync(transaksi.PerangkatId);
            if (perangkat != null)
            {
                if (perangkat.Jumlah < transaksi.Jumlah) return (false, "Stok perangkat tidak mencukupi untuk disetujui!");
                perangkat.Jumlah -= transaksi.Jumlah;
            }

            transaksi.Status = StatusTransaksi.Disetujui;
            await _context.SaveChangesAsync();
            await _notificationService.KirimNotifikasiAsync("Menyetujui", "Barang Keluar", $"Menyetujui barang keluar ID {id} sebanyak {transaksi.Jumlah} unit");
            return (true, "Berhasil disetujui.");
        }

        public async Task<bool> TolakKeluarAsync(int id)
        {
            var transaksi = await _context.BarangKeluars.FindAsync(id);
            if (transaksi == null || transaksi.Status != StatusTransaksi.Menunggu) return false;

            transaksi.Status = StatusTransaksi.Ditolak;
            await _context.SaveChangesAsync();
            await _notificationService.KirimNotifikasiAsync("Menolak", "Barang Keluar", $"Menolak pengajuan barang keluar ID {id} sebanyak {transaksi.Jumlah} unit");
            return true;
        }

        // Form Submission Logic
        public async Task SubmitMasukAsync(BarangMasuk model)
        {
            model.Status = StatusTransaksi.Menunggu;
            _context.BarangMasuks.Add(model);
            await _context.SaveChangesAsync();

            var perangkat = await _context.DataPerangkat.FindAsync(model.PerangkatId);
            string namaBarang = perangkat != null ? perangkat.Nama : $"ID {model.PerangkatId}";
            await _notificationService.KirimNotifikasiAsync("Mengajukan", "Barang Masuk (Menunggu)", $"Mengajukan barang masuk untuk perangkat {namaBarang} sebanyak {model.Jumlah} unit");
        }

        public async Task SubmitKeluarAsync(BarangKeluar model)
        {
            model.Status = StatusTransaksi.Menunggu;
            _context.BarangKeluars.Add(model);
            await _context.SaveChangesAsync();

            var perangkat = await _context.DataPerangkat.FindAsync(model.PerangkatId);
            string namaBarang = perangkat != null ? perangkat.Nama : $"ID {model.PerangkatId}";
            await _notificationService.KirimNotifikasiAsync("Mengajukan", "Barang Keluar (Menunggu)", $"Mengajukan barang keluar untuk perangkat {namaBarang} sebanyak {model.Jumlah} unit");
        }

        // Report Generation Logic (PDF & Excel)
        public byte[] GeneratePdfReport(DateTime start, DateTime end)
        {
#pragma warning disable CS8602
            var dataMasuk = _context.BarangMasuks
                .Include(b => b.Perangkat).ThenInclude(p => p.Vendor)
                .Where(b => b.Status == StatusTransaksi.Disetujui || b.Status == StatusTransaksi.Ditolak)
                .ToList();

            var dataKeluar = _context.BarangKeluars
                .Include(b => b.Perangkat).ThenInclude(p => p.Vendor)
                .Where(b => b.Status == StatusTransaksi.Disetujui || b.Status == StatusTransaksi.Ditolak)
                .ToList();

            var listMasuk = dataMasuk.Select(b => new TransaksiHistoryItemResponseDto
            {
                Status = b.Status.ToString(),
                Tanggal = b.TanggalMasuk,
                NamaBarang = b.Perangkat != null ? b.Perangkat.Nama : "Barang Dihapus",
                Jenis = "Masuk",
                Jumlah = b.Jumlah,
                Vendor = (b.Perangkat != null && b.Perangkat.Vendor != null) ? b.Perangkat.Vendor.NamaVendor : "-",
                Penerima = "-",
                Keterangan = b.Keterangan
            });

            var listKeluar = dataKeluar.Select(b => new TransaksiHistoryItemResponseDto
            {
                Status = b.Status.ToString(),
                Tanggal = b.TanggalKeluar,
                NamaBarang = b.Perangkat != null ? b.Perangkat.Nama : "Barang Dihapus",
                Jenis = "Keluar",
                Jumlah = b.Jumlah,
                Vendor = (b.Perangkat != null && b.Perangkat.Vendor != null) ? b.Perangkat.Vendor.NamaVendor : "-",
                Penerima = b.Penerima ?? "-",
                Keterangan = b.Keterangan
            });
#pragma warning restore CS8602

            var data = listMasuk.Concat(listKeluar)
                .Where(x => x.Tanggal.Date >= start.Date && x.Tanggal.Date <= end.Date)
                .OrderByDescending(x => x.Tanggal)
                .ToList();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(column =>
                        {
                            column.Item().Text("LAPORAN TRANSAKSI GUDANG").FontSize(18).Bold().FontColor("#ED1B24");
                            column.Item().Text($"Periode: {start:dd MMM yyyy} - {end:dd MMM yyyy}").FontSize(10).FontColor(Colors.Grey.Darken2);
                        });
                    });

                    page.Content().PaddingVertical(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(70);
                            columns.RelativeColumn();
                            columns.ConstantColumn(45);
                            columns.ConstantColumn(55);
                            columns.ConstantColumn(35);
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text("Waktu");
                            header.Cell().Element(CellStyle).Text("Barang");
                            header.Cell().Element(CellStyle).Text("Jenis");
                            header.Cell().Element(CellStyle).Text("Status");
                            header.Cell().Element(CellStyle).Text("Qty");
                            header.Cell().Element(CellStyle).Text("Vendor");
                            header.Cell().Element(CellStyle).Text("Penerima");
                            header.Cell().Element(CellStyle).Text("Keterangan");

                            static IContainer CellStyle(IContainer container) => container
                                .DefaultTextStyle(x => x.SemiBold().FontSize(8).FontColor(Colors.White))
                                .PaddingVertical(6)
                                .Background("#003876")
                                .AlignCenter();
                        });

                        foreach (var item in data)
                        {
                            table.Cell().Element(RowStyle).Text(item.Tanggal.ToString("dd MMM yyyy"));
                            table.Cell().Element(RowStyle).Text(item.NamaBarang);
                            table.Cell().Element(RowStyle).Text(item.Jenis);
                            table.Cell().Element(RowStyle).Text(item.Status);
                            table.Cell().Element(RowStyle).Text(item.Jumlah.ToString());
                            table.Cell().Element(RowStyle).Text(item.Vendor);
                            table.Cell().Element(RowStyle).Text(item.Penerima);
                            table.Cell().Element(RowStyle).Text(item.Keterangan ?? "-");

                            static IContainer RowStyle(IContainer container) => container
                                .DefaultTextStyle(x => x.FontSize(8))
                                .BorderBottom(1)
                                .BorderColor(Colors.Grey.Lighten2)
                                .PaddingVertical(5)
                                .AlignCenter();
                        }
                    });
                });
            });

            return document.GeneratePdf();
        }

        public byte[] GenerateExcelReport(DateTime start, DateTime end)
        {
#pragma warning disable CS8602
            var dataMasuk = _context.BarangMasuks
                .Include(b => b.Perangkat).ThenInclude(p => p.Vendor)
                .Where(b => b.Status == StatusTransaksi.Disetujui || b.Status == StatusTransaksi.Ditolak)
                .ToList();

            var dataKeluar = _context.BarangKeluars
                .Include(b => b.Perangkat).ThenInclude(p => p.Vendor)
                .Where(b => b.Status == StatusTransaksi.Disetujui || b.Status == StatusTransaksi.Ditolak)
                .ToList();

            var listMasuk = dataMasuk.Select(b => new TransaksiHistoryItemResponseDto
            {
                Status = b.Status.ToString(),
                Tanggal = b.TanggalMasuk,
                NamaBarang = b.Perangkat != null ? b.Perangkat.Nama : "Barang Dihapus",
                Jenis = "Masuk",
                Jumlah = b.Jumlah,
                Vendor = (b.Perangkat != null && b.Perangkat.Vendor != null) ? b.Perangkat.Vendor.NamaVendor : "-",
                Penerima = "-",
                Keterangan = b.Keterangan
            });

            var listKeluar = dataKeluar.Select(b => new TransaksiHistoryItemResponseDto
            {
                Status = b.Status.ToString(),
                Tanggal = b.TanggalKeluar,
                NamaBarang = b.Perangkat != null ? b.Perangkat.Nama : "Barang Dihapus",
                Jenis = "Keluar",
                Jumlah = b.Jumlah,
                Vendor = (b.Perangkat != null && b.Perangkat.Vendor != null) ? b.Perangkat.Vendor.NamaVendor : "-",
                Penerima = b.Penerima ?? "-",
                Keterangan = b.Keterangan
            });
#pragma warning restore CS8602

            var data = listMasuk.Concat(listKeluar)
                .Where(x => x.Tanggal.Date >= start.Date && x.Tanggal.Date <= end.Date)
                .OrderByDescending(x => x.Tanggal)
                .ToList();

            ExcelPackage.License.SetNonCommercialPersonal("PTK SIGMA");

            using var package = new ExcelPackage();
            var sheet = package.Workbook.Worksheets.Add("Laporan Transaksi");

            sheet.Cells["A1"].Value = "LAPORAN TRANSAKSI GUDANG";
            sheet.Cells["A1"].Style.Font.Bold = true;
            sheet.Cells["A1"].Style.Font.Size = 14;

            sheet.Cells["A2"].Value = $"Periode: {start:dd MMM yyyy} - {end:dd MMM yyyy}";
            sheet.Cells["A2"].Style.Font.Italic = true;

            string[] headers = { "Waktu", "Barang", "Jenis", "Status", "Qty", "Vendor", "Penerima", "Keterangan" };
            for (int i = 0; i < headers.Length; i++)
            {
                sheet.Cells[4, i + 1].Value = headers[i];
            }

            using (var range = sheet.Cells["A4:H4"])
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
                sheet.Cells[row, 1].Value = item.Tanggal.ToString("dd MMM yyyy");
                sheet.Cells[row, 2].Value = item.NamaBarang;
                sheet.Cells[row, 3].Value = item.Jenis;
                sheet.Cells[row, 4].Value = item.Status;
                sheet.Cells[row, 5].Value = item.Jumlah;
                sheet.Cells[row, 6].Value = item.Vendor;
                sheet.Cells[row, 7].Value = item.Penerima;
                sheet.Cells[row, 8].Value = item.Keterangan ?? "-";
                row++;
            }

            sheet.Cells.AutoFitColumns();
            return package.GetAsByteArray();
        }

        public async Task<Lokasi?> GetLokasiByIdAsync(int id)
            => await _context.Lokasis.FindAsync(id);

        public async Task<Rak?> GetRakByIdAsync(int id)
            => await _context.Raks.FindAsync(id);

        public async Task<object> GetTransactionOptionsAsync()
        {
            var perangkatList = await _context.DataPerangkat
                .Where(p => !p.IsDeleted)
                .Select(p => new
                {
                    Id = p.Id,
                    Nama = p.Nama,
                    Kategori = p.Kategori,
                    SnLaptop = p.SnLaptop,
                    Jumlah = p.Jumlah
                })
                .ToListAsync();

            var kategoriList = await _context.Kategoris
                .Select(k => k.NamaKategori)
                .ToListAsync();

            return new { perangkatList, kategoriList };
        }

    }
}
