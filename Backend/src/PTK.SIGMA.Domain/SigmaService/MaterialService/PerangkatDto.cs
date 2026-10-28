using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace PTK.SIGMA.Domain.SigmaService
{
    public class PerangkatResponseDto
    {
        public int Id { get; set; }
        public string? ProductId { get; set; }
        public string? Nama { get; set; }
        public string? Kategori { get; set; }
        public string? StatusSewa { get; set; }
        public string? TypeLaptop { get; set; }
        public string? Kondisi { get; set; }
        public string? SnLaptop { get; set; }
        public int Jumlah { get; set; }
        public string? LokasiRak { get; set; }
        public int? LokasiId { get; set; }
        public string NamaLokasi { get; set; } = string.Empty;
        public int? VendorId { get; set; }
        public string NamaVendor { get; set; } = string.Empty;
        public string? Dokumentasi { get; set; }
        public string? Keterangan { get; set; }
        public bool IsVerified { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }

    public class PerangkatCreateDto
    {
        [Required(ErrorMessage = "Nama perangkat wajib diisi!")]
        public string? Nama { get; set; }

        public string? Kategori { get; set; }
        public string? StatusSewa { get; set; }
        public string? TypeLaptop { get; set; }
        public string? ProductId { get; set; }
        public string? Kondisi { get; set; }
        public string? SnLaptop { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Jumlah tidak boleh negatif!")]
        public int Jumlah { get; set; } = 0;

        public int? LokasiId { get; set; }
        public string? LokasiRak { get; set; }
        public int? VendorId { get; set; }

        public IFormFile? FileDokumentasi { get; set; }

        public string? Keterangan { get; set; }
    }
}
