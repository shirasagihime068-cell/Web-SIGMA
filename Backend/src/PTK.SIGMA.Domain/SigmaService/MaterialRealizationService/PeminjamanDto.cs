using System;
using System.ComponentModel.DataAnnotations;

namespace PTK.SIGMA.Domain.SigmaService
{
    // DTO untuk mengirim data riwayat/detail peminjaman ke frontend (Response)
    public class PeminjamanResponseDto
    {
        public int Id { get; set; }
        public string? NamaPeminjam { get; set; }
        public string? Jabatan { get; set; }
        public string? Fungsi { get; set; }
        public int PerangkatId { get; set; }
        public string NamaPerangkat { get; set; } = string.Empty;
        public string? SerialNumber { get; set; }
        public int Jumlah { get; set; }
        public int? LokasiId { get; set; }
        public string NamaLokasi { get; set; } = string.Empty;
        public DateTime TanggalPinjam { get; set; }
        public DateTime? TanggalKembali { get; set; }
        public string? Status { get; set; }
        public string? StatusPeminjaman { get; set; }
        public string? Keterangan { get; set; }
        public string? NoBa { get; set; }
    }

    // DTO untuk menerima data saat user/staff mengajukan/mengedit peminjaman (Create/Edit Request)
    public class PeminjamanCreateDto
    {
        [Required(ErrorMessage = "Nama peminjam wajib diisi!")]
        public string? NamaPeminjam { get; set; }

        public string? Jabatan { get; set; }
        public string? Fungsi { get; set; }

        [Required(ErrorMessage = "Pilih perangkat terlebih dahulu!")]
        public int PerangkatId { get; set; }

        public string? SerialNumber { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Jumlah minimal adalah 1!")]
        public int Jumlah { get; set; } = 1;

        public int? LokasiId { get; set; }

        // --- TAMBAHKAN DUA PROPERTI INI ---
        public DateTime TanggalPinjam { get; set; }
        public DateTime? TanggalKembali { get; set; }
        // ----------------------------------

        public string? Keterangan { get; set; }
        public string? NoBa { get; set; }
    }
}
