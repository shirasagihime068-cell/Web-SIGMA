using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace PTK.SIGMA.Domain.SigmaService
{
    // ==========================================
    // 1. STOCK OPNAME RESPONSE DTO
    // ==========================================
    public class StockOpnameResponseDto
    {
        public int Id { get; set; }
        public int PerangkatId { get; set; }
        public string NamaPerangkat { get; set; } = string.Empty; // Menghindari circular reference
        public int JumlahSistem { get; set; }
        public int JumlahFisik { get; set; }
        public int Selisih { get; set; }
        public string KondisiFisik { get; set; } = string.Empty;
        public string? Keterangan { get; set; }
        public string? FotoOpname { get; set; }
        public DateTime TanggalOpname { get; set; }
    }

    // ==========================================
    // 2. STOCK OPNAME CREATE / UPDATE DTO
    // ==========================================
    public class StockOpnameCreateDto
    {
        [Required(ErrorMessage = "Perangkat wajib dipilih!")]
        public int PerangkatId { get; set; }

        public int JumlahSistem { get; set; } = 1;

        [Required(ErrorMessage = "Jumlah fisik wajib diisi!")]
        [Range(0, int.MaxValue, ErrorMessage = "Jumlah fisik tidak boleh negatif!")]
        public int JumlahFisik { get; set; }

        [Required(ErrorMessage = "Kondisi fisik wajib dipilih!")]
        public string KondisiFisik { get; set; } = string.Empty;

        public string? Keterangan { get; set; }

        // Menangani upload foto fisik opname via API
        public IFormFile? FileFotoOpname { get; set; }
    }
}
