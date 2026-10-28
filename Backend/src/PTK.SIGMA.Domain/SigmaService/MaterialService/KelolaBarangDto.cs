using System;
using System.ComponentModel.DataAnnotations;

namespace PTK.SIGMA.Domain.SigmaService
{
    // ==========================================
    // 1. MASTER DATA DTOs (Kategori, Lokasi, Rak)
    // ==========================================

    public class KategoriResponseDto
    {
        public int Id { get; set; }
        public string NamaKategori { get; set; } = string.Empty;
    }

    public class KategoriCreateDto
    {
        [Required(ErrorMessage = "Nama kategori wajib diisi!")]
        public string NamaKategori { get; set; } = string.Empty;
    }

    public class LokasiResponseDto
    {
        public int Id { get; set; }
        public string NamaLokasi { get; set; } = string.Empty;
        public string? Keterangan { get; set; } // Ditambahkan agar sinkron dengan entitas Lokasi
    }

    public class LokasiCreateDto
    {
        [Required(ErrorMessage = "Nama lokasi wajib diisi!")]
        public string NamaLokasi { get; set; } = string.Empty;
        public string? Keterangan { get; set; } // Ditambahkan agar sinkron dengan entitas Lokasi
    }

    public class RakResponseDto
    {
        public int Id { get; set; }
        public string NamaRak { get; set; } = string.Empty;
    }

    public class RakCreateDto
    {
        [Required(ErrorMessage = "Nama rak wajib diisi!")]
        public string NamaRak { get; set; } = string.Empty;
    }

    // ==========================================
    // 2. TRANSAKSI BARANG MASUK DTOs
    // ==========================================

    public class BarangMasukResponseDto
    {
        public int Id { get; set; }
        public DateTime TanggalMasuk { get; set; }
        public int PerangkatId { get; set; }
        public string NamaPerangkat { get; set; } = string.Empty;
        public int Jumlah { get; set; }
        public int? VendorId { get; set; }
        public string NamaVendor { get; set; } = string.Empty;
        public string? Keterangan { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class BarangMasukCreateDto
    {
        [Required(ErrorMessage = "Perangkat wajib dipilih!")]
        public int PerangkatId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Jumlah minimal adalah 1!")]
        public int Jumlah { get; set; }

        public int? VendorId { get; set; }
        public string? Keterangan { get; set; }
    }

    // ==========================================
    // 3. TRANSAKSI BARANG KELUAR DTOs
    // ==========================================

    public class BarangKeluarResponseDto
    {
        public int Id { get; set; }
        public DateTime TanggalKeluar { get; set; }
        public int PerangkatId { get; set; }
        public string NamaPerangkat { get; set; } = string.Empty;
        public int Jumlah { get; set; }
        public string? Penerima { get; set; }
        public string? Keterangan { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class BarangKeluarCreateDto
    {
        [Required(ErrorMessage = "Perangkat wajib dipilih!")]
        public int PerangkatId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Jumlah minimal adalah 1!")]
        public int Jumlah { get; set; }

        [Required(ErrorMessage = "Nama penerima wajib diisi!")]
        public string? Penerima { get; set; }

        public string? Keterangan { get; set; }
    }

    // ==========================================
    // 4. RIWAYAT TRANSAKSI GABUNGAN (INDEX / REPORT)
    // ==========================================

    public class TransaksiHistoryItemResponseDto
    {
        public int Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime Tanggal { get; set; }
        public string? NamaBarang { get; set; }
        public string? Jenis { get; set; } // "Masuk" atau "Keluar"
        public int Jumlah { get; set; }
        public string? Vendor { get; set; }
        public string? Penerima { get; set; }
        public string? Keterangan { get; set; }
    }
}
