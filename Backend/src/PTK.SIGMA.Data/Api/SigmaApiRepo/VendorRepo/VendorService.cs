using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PTK.SIGMA.Data.Context;
using PTK.SIGMA.Domain.Entities;
using PTK.SIGMA.Domain.SigmaService; 



namespace PTK.SIGMA.Data.Api.SigmaApiRepo
{
    public class VendorService : IVendorService
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;

        public VendorService(AppDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<List<VendorResponseDto>> GetVendorsAsync()
        {
            var vendors = await _context.Vendors.ToListAsync();
            return vendors.Select(v => new VendorResponseDto
            {
                Id = v.Id,
                NamaVendor = v.NamaVendor ?? string.Empty
            }).ToList();
        }

        public async Task<VendorResponseDto?> GetVendorByIdAsync(int id)
        {
            var vendor = await _context.Vendors.FindAsync(id);
            if (vendor == null) return null;

            return new VendorResponseDto
            {
                Id = vendor.Id,
                NamaVendor = vendor.NamaVendor ?? string.Empty
            };
        }

        public async Task<(bool Success, string Message)> CreateVendorAsync(VendorCreateDto dto)
        {
            var vendor = new Vendor
            {
                NamaVendor = dto.NamaVendor
            };

            _context.Vendors.Add(vendor);
            await _context.SaveChangesAsync();

            await _notificationService.KirimNotifikasiAsync(
                "Menambah",
                "Vendor",
                $"Menambahkan vendor baru: {vendor.NamaVendor}"
            );

            return (true, "Vendor berhasil ditambahkan!");
        }

        public async Task<(bool Success, string Message)> UpdateVendorAsync(int id, VendorCreateDto dto)
        {
            var vendor = await _context.Vendors.FindAsync(id);
            if (vendor == null)
            {
                return (false, "Vendor tidak ditemukan.");
            }

            vendor.NamaVendor = dto.NamaVendor;

            _context.Vendors.Update(vendor);
            await _context.SaveChangesAsync();

            await _notificationService.KirimNotifikasiAsync(
                "Mengubah",
                "Vendor",
                $"Memperbarui vendor: {vendor.NamaVendor}"
            );

            return (true, "Vendor berhasil diperbarui!");
        }

        public async Task<bool> DeleteVendorAsync(int id)
        {
            var vendor = await _context.Vendors.FindAsync(id);
            if (vendor == null) return false;

            _context.Vendors.Remove(vendor);
            await _context.SaveChangesAsync();

            await _notificationService.KirimNotifikasiAsync(
                "Menghapus",
                "Vendor",
                $"Menghapus vendor: {vendor.NamaVendor}"
            );

            return true;
        }
    }
}
