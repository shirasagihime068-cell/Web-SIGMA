using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PTK.SIGMA.Data.Context;
using PTK.SIGMA.Domain.Entities;
using PTK.SIGMA.Domain.SigmaService;

namespace PTK.SIGMA.Data.Api.SigmaApiRepo
{
    public class PerangkatService : IPerangkatService
    {
        private readonly AppDbContext _context;
        private readonly IFileStorage _fileStorage;
        private readonly INotificationService _notificationService;

        public PerangkatService(AppDbContext context, IFileStorage fileStorage, INotificationService notificationService)
        {
            _context = context;
            _fileStorage = fileStorage;
            _notificationService = notificationService;
        }

        public async Task<List<PerangkatResponseDto>> GetCetakLaporanDataAsync()
        {
            var data = await _context.DataPerangkat
                .AsNoTracking()
                .Include(p => p.Vendor)
                .Include(p => p.Lokasi)
                .Where(p => p.IsDeleted == false && p.IsVerified == true)
                .ToListAsync();

            return data.Select(MapToResponseDto).ToList();
        }

        public async Task<object> GetIndexDataAsync(string? searchString, string? searchCategory, int? page)
        {
            var kategoriList = await _context.Kategoris
                .Select(k => k.NamaKategori)
                .ToListAsync();

            var perangkatQuery = _context.DataPerangkat
                .AsNoTracking()
                .Include(p => p.Vendor)
                .Include(p => p.Lokasi)
                .Where(p => p.IsDeleted == false && p.IsVerified == true);

            if (!string.IsNullOrEmpty(searchCategory))
            {
                if (searchCategory == "Perangkat ICT Operation")
                {
                    perangkatQuery = perangkatQuery.Where(p => string.IsNullOrEmpty(p.Kategori) || p.Kategori == searchCategory);
                }
                else
                {
                    perangkatQuery = perangkatQuery.Where(p => p.Kategori == searchCategory);
                }
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                perangkatQuery = perangkatQuery.Where(p =>
                    (!string.IsNullOrEmpty(p.Nama) && p.Nama.Contains(searchString)) ||
                    (!string.IsNullOrEmpty(p.SnLaptop) && p.SnLaptop.Contains(searchString)) ||
                    (!string.IsNullOrEmpty(p.Keterangan) && p.Keterangan.Contains(searchString)) ||
                    (p.Lokasi != null && !string.IsNullOrEmpty(p.Lokasi.NamaLokasi) && p.Lokasi.NamaLokasi.Contains(searchString))
                );
            }

            int pageSize = 15;
            int pageNumber = Math.Max(1, page ?? 1);
            int totalItems = await perangkatQuery.CountAsync();
            int totalPages = Math.Max(1, (int)Math.Ceiling(decimal.Divide(totalItems, pageSize)));
            pageNumber = Math.Min(pageNumber, totalPages);

            var items = await perangkatQuery
                .OrderByDescending(p => p.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var itemDtos = items.Select(MapToResponseDto).ToList();

            return new
            {
                kategoriList,
                pagination = new { currentPage = pageNumber, totalPages, totalItems },
                data = itemDtos
            };
        }

        public async Task<(bool Success, string Message)> CreatePerangkatAsync(PerangkatCreateDto dto, string currentUserName)
        {
            string? namaFile = null;

            if (dto.FileDokumentasi != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".heic" };
                var extension = Path.GetExtension(dto.FileDokumentasi.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                {
                    return (false, "Hanya format gambar (.jpg, .jpeg, .png, .heic) yang diizinkan!");
                }

                if (dto.FileDokumentasi.Length > 50 * 1024 * 1024)
                {
                    return (false, "Ukuran gambar maksimal adalah 50MB!");
                }

                namaFile = Guid.NewGuid().ToString() + extension;

                await using var fileStream = dto.FileDokumentasi.OpenReadStream();
                namaFile = await _fileStorage.UploadPerangkatImageAsync(
                    fileStream,
                    namaFile,
                    dto.FileDokumentasi.ContentType ?? "application/octet-stream");
            }

            var perangkat = new Perangkat
            {
                Nama = dto.Nama ?? string.Empty,
                Kategori = dto.Kategori ?? string.Empty,
                StatusSewa = dto.StatusSewa,
                TypeLaptop = dto.TypeLaptop,
                ProductId = dto.ProductId,
                SnLaptop = dto.SnLaptop,
                Jumlah = dto.Jumlah,
                LokasiRak = dto.LokasiRak,
                LokasiId = dto.LokasiId,
                VendorId = dto.VendorId,
                Dokumentasi = namaFile,
                Keterangan = dto.Keterangan,
                Kondisi = dto.Kondisi ?? string.Empty,
                CreatedAt = DateTime.Now,
                CreatedBy = currentUserName,
                IsVerified = false,
                IsDeleted = false
            };

            _context.DataPerangkat.Add(perangkat);
            await _context.SaveChangesAsync();

            await _notificationService.KirimNotifikasiAsync(
                "Menambah",
                "Perangkat",
                $"(Pending Validasi) {perangkat.CreatedBy} menambahkan perangkat baru: {perangkat.Nama}"
            );

            return (true, "Data barang berhasil ditambahkan dan menunggu validasi Admin!");
        }

        public async Task<bool> SoftDeleteAsync(int id, string currentUserName)
        {
            var perangkat = await _context.DataPerangkat.FindAsync(id);
            if (perangkat == null) return false;

            perangkat.IsDeleted = true;
            perangkat.UpdatedAt = DateTime.Now;
            perangkat.UpdatedBy = currentUserName;

            _context.Update(perangkat);
            await _context.SaveChangesAsync();

            await _notificationService.KirimNotifikasiAsync(
                "Mengarsipkan",
                "Perangkat",
                $"Mengarsipkan perangkat: {perangkat.Nama}"
            );

            return true;
        }

        public async Task<(bool Success, string Message)> EditPerangkatAsync(int id, PerangkatCreateDto dto, string currentUserName)
        {
            var existingPerangkat = await _context.DataPerangkat.FindAsync(id);
            if (existingPerangkat == null) return (false, "Data perangkat tidak ditemukan.");

            string? namaFile = existingPerangkat.Dokumentasi;

            if (dto.FileDokumentasi != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".heic" };
                var extension = Path.GetExtension(dto.FileDokumentasi.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                {
                    return (false, "Hanya format gambar (.jpg, .jpeg, .png, .heic) yang diizinkan!");
                }

                if (dto.FileDokumentasi.Length > 50 * 1024 * 1024)
                {
                    return (false, "Ukuran gambar maksimal adalah 50MB!");
                }

                var oldFileName = existingPerangkat.Dokumentasi;
                var newFileName = Guid.NewGuid().ToString() + extension;

                await using var fileStream = dto.FileDokumentasi.OpenReadStream();
                newFileName = await _fileStorage.UploadPerangkatImageAsync(
                    fileStream,
                    newFileName,
                    dto.FileDokumentasi.ContentType ?? "application/octet-stream");

                if (!string.IsNullOrWhiteSpace(oldFileName))
                {
                    await _fileStorage.DeletePerangkatImageAsync(oldFileName);
                }

                namaFile = newFileName;
            }

            existingPerangkat.Nama = dto.Nama ?? string.Empty;
            existingPerangkat.Kategori = dto.Kategori ?? string.Empty;
            existingPerangkat.StatusSewa = dto.StatusSewa;
            existingPerangkat.TypeLaptop = dto.TypeLaptop;
            existingPerangkat.ProductId = dto.ProductId;
            existingPerangkat.Kondisi = dto.Kondisi ?? string.Empty;
            existingPerangkat.SnLaptop = dto.SnLaptop;
            existingPerangkat.Jumlah = dto.Jumlah;
            existingPerangkat.LokasiRak = dto.LokasiRak;
            existingPerangkat.LokasiId = dto.LokasiId;
            existingPerangkat.VendorId = dto.VendorId;
            existingPerangkat.Dokumentasi = namaFile;
            existingPerangkat.Keterangan = dto.Keterangan;
            existingPerangkat.UpdatedAt = DateTime.Now;
            existingPerangkat.UpdatedBy = currentUserName;

            _context.Update(existingPerangkat);
            await _context.SaveChangesAsync();

            await _notificationService.KirimNotifikasiAsync(
                "Mengubah",
                "Perangkat",
                $"Memperbarui data perangkat: {existingPerangkat.Nama}"
            );

            return (true, "Data barang berhasil diperbarui!");
        }

        public async Task<List<PerangkatResponseDto>> GetArsipDataAsync()
        {
            var data = await _context.DataPerangkat
                .Include(p => p.Vendor)
                .Include(p => p.Lokasi)
                .Where(p => p.IsDeleted == true)
                .ToListAsync();

            return data.Select(MapToResponseDto).ToList();
        }

        public async Task<bool> RestoreAsync(int id, string currentUserName)
        {
            var perangkat = await _context.DataPerangkat.FindAsync(id);
            if (perangkat == null) return false;

            perangkat.IsDeleted = false;
            perangkat.UpdatedAt = DateTime.Now;
            perangkat.UpdatedBy = currentUserName;

            _context.Update(perangkat);
            await _context.SaveChangesAsync();

            await _notificationService.KirimNotifikasiAsync(
                "Memulihkan",
                "Perangkat",
                $"Memulihkan perangkat dari arsip: {perangkat.Nama}"
            );

            return true;
        }

        public async Task<bool> PermanentDeleteAsync(int id)
        {
            var perangkat = await _context.DataPerangkat.FindAsync(id);
            if (perangkat == null) return false;

            _context.DataPerangkat.Remove(perangkat);
            await _context.SaveChangesAsync();

            await _notificationService.KirimNotifikasiAsync(
                "Menghapus Permanen",
                "Perangkat",
                $"Menghapus permanen perangkat ID: {perangkat.Id} ({perangkat.Nama})"
            );

            return true;
        }

        public async Task<List<PerangkatResponseDto>> GetValidasiPendingDataAsync()
        {
            var data = await _context.DataPerangkat
                .Include(p => p.Vendor)
                .Include(p => p.Lokasi)
                .Where(p => p.IsDeleted == false && p.IsVerified == false)
                .ToListAsync();

            return data.Select(MapToResponseDto).ToList();
        }

        public async Task<bool> SetujuiValidasiAsync(int id, string currentUserName)
        {
            var perangkat = await _context.DataPerangkat.FindAsync(id);
            if (perangkat == null) return false;

            perangkat.IsVerified = true;
            perangkat.UpdatedAt = DateTime.Now;
            perangkat.UpdatedBy = currentUserName;

            _context.Update(perangkat);
            await _context.SaveChangesAsync();

            await _notificationService.KirimNotifikasiAsync(
                "Memvalidasi",
                "Perangkat",
                $"Barang baru '{perangkat.Nama}' telah divalidasi dan disetujui oleh Admin. Stok resmi masuk sistem."
            );

            return true;
        }

        public async Task<bool> TolakValidasiAsync(int id)
        {
            var perangkat = await _context.DataPerangkat.FindAsync(id);
            if (perangkat == null) return false;

            string namaBarang = perangkat.Nama;
            _context.DataPerangkat.Remove(perangkat);
            await _context.SaveChangesAsync();

            await _notificationService.KirimNotifikasiAsync(
                "Menolak",
                "Perangkat",
                $"Barang '{namaBarang}' ditolak dan dihapus oleh Admin."
            );

            return true;
        }

        // Helper untuk mapping Entity Perangkat ke PerangkatResponseDto
        private static PerangkatResponseDto MapToResponseDto(Perangkat p)
        {
            return new PerangkatResponseDto
            {
                Id = p.Id,
                Nama = p.Nama,
                Kategori = p.Kategori,
                StatusSewa = p.StatusSewa,
                TypeLaptop = p.TypeLaptop,
                ProductId = p.ProductId,
                SnLaptop = p.SnLaptop,
                Jumlah = p.Jumlah,
                LokasiRak = p.LokasiRak,
                LokasiId = p.LokasiId,
                NamaLokasi = p.Lokasi?.NamaLokasi ?? "-",
                VendorId = p.VendorId,
                NamaVendor = p.Vendor?.NamaVendor ?? "-",
                Dokumentasi = p.Dokumentasi,
                Keterangan = p.Keterangan,
                Kondisi = p.Kondisi,
                IsVerified = p.IsVerified,
                IsDeleted = p.IsDeleted,
                CreatedAt = p.CreatedAt,
                CreatedBy = p.CreatedBy,
                UpdatedAt = p.UpdatedAt,
                UpdatedBy = p.UpdatedBy
            };
        }

        public async Task<object> GetCreateOptionsAsync()
        {
            var vendorList = await _context.Vendors
                .AsNoTracking()
                .Select(v => new { v.Id, v.NamaVendor })
                .ToListAsync();

            var lokasiList = await _context.Lokasis
                .AsNoTracking()
                .Select(l => new { l.Id, l.NamaLokasi })
                .ToListAsync();

            var kategoriList = await _context.Kategoris
                .AsNoTracking()
                .Select(k => k.NamaKategori)
                .ToListAsync();

            var lokasiRakList = await _context.Raks
                .AsNoTracking()
                .Select(r => r.NamaRak)
                .OrderBy(r => r)
                .ToListAsync();

            return new { vendorList, lokasiList, kategoriList, lokasiRakList };
        }

        public async Task<PerangkatResponseDto?> GetByIdAsync(int id)
        {
            var perangkat = await _context.DataPerangkat
                .AsNoTracking()
                .Include(p => p.Vendor)
                .Include(p => p.Lokasi)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (perangkat == null) return null;

            return new PerangkatResponseDto
            {
                Id = perangkat.Id,
                Nama = perangkat.Nama,
                Kategori = perangkat.Kategori,
                StatusSewa = perangkat.StatusSewa,
                TypeLaptop = perangkat.TypeLaptop,
                ProductId = perangkat.ProductId,
                SnLaptop = perangkat.SnLaptop,
                Jumlah = perangkat.Jumlah,
                LokasiRak = perangkat.LokasiRak,
                LokasiId = perangkat.LokasiId,
                NamaLokasi = perangkat.Lokasi?.NamaLokasi ?? "-",
                VendorId = perangkat.VendorId,
                NamaVendor = perangkat.Vendor?.NamaVendor ?? "-",
                Dokumentasi = perangkat.Dokumentasi,
                Keterangan = perangkat.Keterangan,
                Kondisi = perangkat.Kondisi,
                IsVerified = perangkat.IsVerified,
                IsDeleted = perangkat.IsDeleted,
                CreatedAt = perangkat.CreatedAt,
                CreatedBy = perangkat.CreatedBy,
                UpdatedAt = perangkat.UpdatedAt,
                UpdatedBy = perangkat.UpdatedBy
            };
        }

        // ==============================================================
        // FUNGSI BARU UNTUK MENGAMBIL FOTO PERANGKAT DARI MINIO
        // ==============================================================
        public async Task<StoredFileDto?> GetPerangkatImageAsync(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return null;

            return await _fileStorage.GetPerangkatImageAsync(fileName);
        }
    }
}
