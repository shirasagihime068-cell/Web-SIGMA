using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using PTK.SIGMA.Domain.SigmaService;

namespace PTK.SIGMA.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public sealed class OpnameController : ControllerBase
    {
        private readonly IOpnameService _opnameService;

        public OpnameController(IOpnameService opnameService)
        {
            _opnameService = opnameService;
        }

        // GET: api/opname
        [HttpGet]
        public async Task<IActionResult> Index(string? searchString, string? lokasiRak, string? kategori, int? bulan, int? tahun)
        {
            if (!IsValidPeriod(bulan, tahun))
                return BadRequest(new { success = false, message = "Periode opname tidak valid." });

            var result = await _opnameService.GetIndexDataAsync(searchString, lokasiRak, kategori, bulan, tahun);
            return Ok(new { success = true, data = result });
        }

        // GET: api/opname/reminder-status
        // Returns the current month's completion status for all active devices.
        [HttpGet("reminder-status")]
        public async Task<IActionResult> ReminderStatus()
        {
            var result = await _opnameService.GetReminderStatusAsync();
            return Ok(new { success = true, data = result });
        }

        // GET: api/opname/arsip
        [HttpGet("arsip")]
        public async Task<IActionResult> Arsip(int? bulan, int? tahun)
        {
            if (!IsValidPeriod(bulan, tahun))
                return BadRequest(new { success = false, message = "Periode opname tidak valid." });

            var result = await _opnameService.GetArsipDataAsync(bulan, tahun);
            return Ok(new { success = true, data = result });
        }

        // POST: api/opname/record
        [HttpPost("record")]
        public async Task<IActionResult> RecordOpname([FromForm] StockOpnameCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Data input tidak valid", errors = ModelState });
            }

            var result = await _opnameService.RecordOpnameAsync(dto);
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message });
        }

        // DELETE: api/opname/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            bool success = await _opnameService.DeleteOpnameAsync(id);
            if (!success) return NotFound(new { success = false, message = "Data riwayat stock opname tidak ditemukan." });

            return Ok(new { success = true, message = "Data riwayat stock opname berhasil dihapus!" });
        }

        // GET: api/opname/download-pdf
        [HttpGet("download-pdf")]
        public async Task<IActionResult> DownloadPdf(string? lokasiRak, string? kategori, int? bulan, int? tahun)
        {
            if (!IsValidPeriod(bulan, tahun))
                return BadRequest(new { success = false, message = "Periode opname tidak valid." });

            byte[] pdfBytes = await _opnameService.GeneratePdfReportAsync(lokasiRak, kategori, bulan, tahun);
            int targetBulan = bulan ?? System.DateTime.Now.Month;
            int targetTahun = tahun ?? System.DateTime.Now.Year;
            string namaBulan = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(targetBulan);

            return File(pdfBytes, "application/pdf", $"Laporan_Stock_Opname_{namaBulan}_{targetTahun}.pdf");
        }

        private static bool IsValidPeriod(int? bulan, int? tahun)
            => (!bulan.HasValue || bulan is >= 1 and <= 12) &&
               (!tahun.HasValue || tahun is >= 2000 and <= 2100);
    }
}
