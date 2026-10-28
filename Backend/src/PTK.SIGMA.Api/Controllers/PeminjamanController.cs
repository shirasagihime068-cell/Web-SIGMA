using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System;
using PTK.SIGMA.Domain.SigmaService;

namespace PTK.SIGMA.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public sealed class PeminjamanController : ControllerBase
    {
        private readonly IPeminjamanService _peminjamanService;

        public PeminjamanController(IPeminjamanService peminjamanService)
        {
            _peminjamanService = peminjamanService;
        }

        // GET: api/peminjaman
        [HttpGet]
        public async Task<IActionResult> Index(string? searchString, DateTime? startDate, DateTime? endDate, string? kategori, int? page)
        {
            var result = await _peminjamanService.GetIndexDataAsync(searchString, startDate, endDate, kategori, page);
            return Ok(new { success = true, data = result });
        }

        // GET: api/peminjaman/download-pdf
        [HttpGet("download-pdf")]
        public async Task<IActionResult> DownloadPdf(DateTime? startDate, DateTime? endDate)
        {
            byte[] pdfBytes = await _peminjamanService.GeneratePdfReportAsync(startDate, endDate);
            string fileName = startDate.HasValue && endDate.HasValue
                ? $"LAPORAN PEMINJAMAN ({startDate.Value:dd-MM-yyyy} s.d {endDate.Value:dd-MM-yyyy}).pdf"
                : "LAPORAN PEMINJAMAN.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }

        // GET: api/peminjaman/download-excel
        [HttpGet("download-excel")]
        public async Task<IActionResult> DownloadExcel(DateTime start, DateTime end)
        {
            if (start == default || end == default || end < start)
                return BadRequest(new { success = false, message = "Rentang tanggal laporan tidak valid." });

            byte[] excelBytes = await _peminjamanService.GenerateExcelReportAsync(start, end);
            string fileName = $"LAPORAN PEMINJAMAN ({start:dd-MM-yyyy} s.d {end:dd-MM-yyyy}).xlsx";
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // GET: api/peminjaman/{id}/print-ba
        [HttpGet("{id}/print-ba")]
        public async Task<IActionResult> PrintBa(int id)
        {
            var result = await _peminjamanService.GetByIdAsync(id);
            if (result == null) return NotFound(new { success = false, message = "Data Berita Acara tidak ditemukan." });

            return Ok(new { success = true, data = result });
        }

        // GET: api/peminjaman/create-options
        [HttpGet("create-options")]
        public async Task<IActionResult> GetCreateOptions()
        {
            var options = await _peminjamanService.GetCreateOptionsAsync();
            return Ok(new { success = true, options });
        }

        // POST: api/peminjaman
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PeminjamanCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Data input tidak valid", errors = ModelState });

            var result = await _peminjamanService.CreatePeminjamanAsync(dto);
            if (!result.Success)
                return BadRequest(new { success = false, message = result.Message });

            return Ok(new { success = true, message = result.Message, data = result.Data });
        }

        // GET: api/peminjaman/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _peminjamanService.GetByIdAsync(id);
            if (result == null) return NotFound(new { success = false, message = "Data peminjaman tidak ditemukan." });

            return Ok(new { success = true, data = result });
        }

        // PUT: api/peminjaman/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [FromBody] PeminjamanCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Data input tidak valid", errors = ModelState });

            var result = await _peminjamanService.UpdatePeminjamanAsync(id, dto);
            if (!result.Success)
                return NotFound(new { success = false, message = result.Message });

            return Ok(new { success = true, message = result.Message });
        }

        // POST: api/peminjaman/{id}/setujui
        [HttpPost("{id}/setujui")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Setujui(int id)
        {
            bool success = await _peminjamanService.SetujuiAsync(id);
            if (!success) return BadRequest(new { success = false, message = "Gagal menyetujui: Stok perangkat di gudang tidak mencukupi atau data tidak ditemukan." });

            return Ok(new { success = true, message = "Pengajuan peminjaman berhasil disetujui!" });
        }

        // POST: api/peminjaman/{id}/tolak
        [HttpPost("{id}/tolak")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Tolak(int id)
        {
            bool success = await _peminjamanService.TolakAsync(id);
            if (!success) return NotFound(new { success = false, message = "Data peminjaman tidak ditemukan." });

            return Ok(new { success = true, message = "Pengajuan peminjaman telah ditolak." });
        }

        // POST: api/peminjaman/{id}/kembalikan
        [HttpPost("{id}/kembalikan")]
        public async Task<IActionResult> Kembalikan(int id)
        {
            bool success = await _peminjamanService.KembalikanAsync(id);
            if (!success) return NotFound(new { success = false, message = "Data peminjaman tidak ditemukan." });

            return Ok(new { success = true, message = "Status peminjaman berhasil diubah menjadi Dikembalikan!" });
        }

        // DELETE: api/peminjaman/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            bool success = await _peminjamanService.DeleteAsync(id);
            if (!success) return NotFound(new { success = false, message = "Data peminjaman tidak ditemukan." });

            return Ok(new { success = true, message = "Data peminjaman berhasil dihapus!" });
        }
    }
}
