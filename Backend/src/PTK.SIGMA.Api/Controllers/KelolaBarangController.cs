using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using PTK.SIGMA.Domain.SigmaService;
using PTK.SIGMA.Domain.Entities;

namespace PTK.SIGMA.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class KelolaBarangController : ControllerBase
{
    private readonly IKelolaBarangService _service;

    public KelolaBarangController(IKelolaBarangService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
        => Ok(new { success = true, data = await _service.GetIndexTransactionsAsync() });

    [HttpGet("kategori")]
    public async Task<IActionResult> GetKategori()
        => Ok(new { success = true, data = await _service.GetKategorisAsync() });

    [HttpPost("kategori")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateKategori([FromBody] KategoriCreateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Data tidak valid", errors = ModelState });

        await _service.AddKategoriAsync(new Kategori { NamaKategori = dto.NamaKategori });
        return Ok(new { success = true, message = "Kategori berhasil ditambahkan!" });
    }

    [HttpGet("kategori/{id}")]
    public async Task<IActionResult> GetKategoriById(int id)
    {
        var kategori = await _service.GetKategoriByIdAsync(id);
        if (kategori == null) return NotFound(new { success = false, message = "Kategori tidak ditemukan." });
        return Ok(new { success = true, data = kategori });
    }

    [HttpPut("kategori/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> EditKategori(int id, [FromBody] KategoriCreateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Data tidak valid", errors = ModelState });

        var kategori = await _service.GetKategoriByIdAsync(id);
        if (kategori == null) return NotFound(new { success = false, message = "Kategori tidak ditemukan." });

        kategori.NamaKategori = dto.NamaKategori;
        await _service.UpdateKategoriAsync(kategori);
        return Ok(new { success = true, message = "Kategori berhasil diperbarui!" });
    }

    [HttpDelete("kategori/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteKategori(int id)
    {
        var kategori = await _service.GetKategoriByIdAsync(id);
        if (kategori == null) return NotFound(new { success = false, message = "Kategori tidak ditemukan." });

        await _service.DeleteKategoriAsync(kategori);
        return Ok(new { success = true, message = "Kategori berhasil dihapus!" });
    }

    [HttpGet("lokasi")]
    public async Task<IActionResult> GetLokasi()
        => Ok(new { success = true, data = await _service.GetLokasisAsync() });

    [HttpPost("lokasi")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> TambahLokasi([FromBody] LokasiCreateDto dto)
    {
        var result = await _service.TambahLokasiAsync(dto?.NamaLokasi ?? string.Empty);
        if (!result.Success) return BadRequest(new { success = false, message = result.Message });
        return Ok(new { success = true, message = result.Message });
    }

    [HttpDelete("lokasi/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> HapusLokasi(int id)
    {
        var lokasi = await _service.GetLokasiByIdAsync(id);
        if (lokasi == null) return NotFound(new { success = false, message = "Data lokasi tidak ditemukan." });

        await _service.HapusLokasiAsync(lokasi);
        return Ok(new { success = true, message = "Lokasi berhasil dihapus dari sistem!" });
    }

    [HttpGet("rak")]
    public async Task<IActionResult> GetRak()
        => Ok(new { success = true, data = await _service.GetRaksAsync() });

    [HttpPost("rak")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> TambahRak([FromBody] RakCreateDto dto)
    {
        var result = await _service.TambahRakAsync(dto?.NamaRak ?? string.Empty);
        if (!result.Success) return BadRequest(new { success = false, message = result.Message });
        return Ok(new { success = true, message = result.Message });
    }

    [HttpDelete("rak/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> HapusRak(int id)
    {
        var rak = await _service.GetRakByIdAsync(id);
        if (rak == null) return NotFound(new { success = false, message = "Data rak tidak ditemukan." });

        await _service.HapusRakAsync(rak);
        return Ok(new { success = true, message = "Lokasi rak berhasil dihapus dari sistem!" });
    }

    [HttpPost("masuk/{id}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ApproveMasuk(int id)
    {
        var success = await _service.ApproveMasukAsync(id);
        if (!success) return NotFound(new { success = false, message = "Transaksi masuk tidak ditemukan atau sudah diproses." });
        return Ok(new { success = true, message = "Barang masuk berhasil disetujui." });
    }

    [HttpPost("masuk/{id}/tolak")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> TolakMasuk(int id)
    {
        var success = await _service.TolakMasukAsync(id);
        if (!success) return NotFound(new { success = false, message = "Transaksi masuk tidak ditemukan atau sudah diproses." });
        return Ok(new { success = true, message = "Barang masuk ditolak." });
    }

    [HttpPost("keluar/{id}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ApproveKeluar(int id)
    {
        var result = await _service.ApproveKeluarAsync(id);
        if (!result.Success) return BadRequest(new { success = false, message = result.Message });
        return Ok(new { success = true, message = result.Message });
    }

    [HttpPost("keluar/{id}/tolak")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> TolakKeluar(int id)
    {
        var success = await _service.TolakKeluarAsync(id);
        if (!success) return NotFound(new { success = false, message = "Transaksi keluar tidak ditemukan atau sudah diproses." });
        return Ok(new { success = true, message = "Barang keluar ditolak." });
    }

    [HttpGet("download-pdf")]
    public IActionResult DownloadPdf(DateTime start, DateTime end)
    {
        if (start == default || end == default || end < start)
            return BadRequest(new { success = false, message = "Rentang tanggal laporan tidak valid." });

        var pdfBytes = _service.GeneratePdfReport(start, end);
        return File(pdfBytes, "application/pdf", $"LAPORAN BARANG KELUAR - MASUK ({start:dd-MM-yyyy} s.d {end:dd-MM-yyyy}).pdf");
    }

    [HttpGet("download-excel")]
    public IActionResult DownloadExcel(DateTime start, DateTime end)
    {
        if (start == default || end == default || end < start)
            return BadRequest(new { success = false, message = "Rentang tanggal laporan tidak valid." });

        var excelBytes = _service.GenerateExcelReport(start, end);
        return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"LAPORAN BARANG KELUAR - MASUK ({start:dd-MM-yyyy} s.d {end:dd-MM-yyyy}).xlsx");
    }

    [HttpGet("masuk/options")]
    public async Task<IActionResult> GetMasukOptions()
        => Ok(new { success = true, data = await _service.GetTransactionOptionsAsync() });

    [HttpPost("masuk")]
    public async Task<IActionResult> Masuk([FromBody] BarangMasukCreateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Data input tidak valid", errors = ModelState });

        await _service.SubmitMasukAsync(new BarangMasuk
        {
            PerangkatId = dto.PerangkatId,
            Jumlah = dto.Jumlah,
            VendorId = dto.VendorId,
            Keterangan = dto.Keterangan
        });

        return Ok(new { success = true, message = "Pengajuan barang masuk berhasil dikirim dan menunggu persetujuan." });
    }

    [HttpGet("keluar/options")]
    public async Task<IActionResult> GetKeluarOptions()
        => Ok(new { success = true, data = await _service.GetTransactionOptionsAsync() });

    [HttpPost("keluar")]
    public async Task<IActionResult> Keluar([FromBody] BarangKeluarCreateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Data input tidak valid", errors = ModelState });

        await _service.SubmitKeluarAsync(new BarangKeluar
        {
            PerangkatId = dto.PerangkatId,
            Jumlah = dto.Jumlah,
            Penerima = dto.Penerima,
            Keterangan = dto.Keterangan
        });

        return Ok(new { success = true, message = "Pengajuan barang keluar berhasil dikirim dan menunggu persetujuan." });
    }
}
