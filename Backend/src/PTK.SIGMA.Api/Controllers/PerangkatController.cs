using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using PTK.SIGMA.Domain.SigmaService;

namespace PTK.SIGMA.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class PerangkatController : ControllerBase
{
    private readonly IPerangkatService _perangkatService;

    public PerangkatController(IPerangkatService perangkatService)
    {
        _perangkatService = perangkatService;
    }

    // ==============================================================================
    // ENDPOINT BARU UNTUK MENGAMBIL GAMBAR DARI MINIO
    // ==============================================================================
    [HttpGet("image/{fileName}")]
    public async Task<IActionResult> GetImage(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return NotFound();

        // Memanggil fungsi GetPerangkatImageAsync melalui _perangkatService
        var fileDto = await _perangkatService.GetPerangkatImageAsync(fileName);

        if (fileDto == null)
            return NotFound(); // Mengembalikan 404 jika gambar tidak ada di MinIO

        // Mengembalikan file gambar ke browser
        return File(fileDto.Content, fileDto.ContentType);
    }
    // ==============================================================================

    [HttpGet("cetak-laporan")]
    public async Task<IActionResult> CetakLaporan()
        => Ok(new { success = true, data = await _perangkatService.GetCetakLaporanDataAsync() });

    [HttpGet]
    public async Task<IActionResult> Index(string? searchString, string? searchCategory, int? page)
        => Ok(new { success = true, data = await _perangkatService.GetIndexDataAsync(searchString, searchCategory, page) });

    [HttpGet("create-options")]
    public async Task<IActionResult> GetCreateOptions()
        => Ok(new { success = true, data = await _perangkatService.GetCreateOptionsAsync() });

    [HttpPost]
    public async Task<IActionResult> Create([FromForm] PerangkatCreateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Data input tidak valid", errors = ModelState });

        var currentUserName = User.Identity?.Name ?? "System";
        var result = await _perangkatService.CreatePerangkatAsync(dto, currentUserName);

        if (!result.Success)
            return BadRequest(new { success = false, message = result.Message });

        return Ok(new { success = true, message = result.Message });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var perangkat = await _perangkatService.GetByIdAsync(id);
        if (perangkat == null)
            return NotFound(new { success = false, message = "Data perangkat tidak ditemukan." });

        return Ok(new { success = true, data = perangkat });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Edit(int id, [FromForm] PerangkatCreateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Data input tidak valid", errors = ModelState });

        var currentUserName = User.Identity?.Name ?? "System";
        var result = await _perangkatService.EditPerangkatAsync(id, dto, currentUserName);

        if (!result.Success)
            return BadRequest(new { success = false, message = result.Message });

        return Ok(new { success = true, message = result.Message });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var currentUserName = User.Identity?.Name ?? "System";
        var success = await _perangkatService.SoftDeleteAsync(id, currentUserName);

        if (!success)
            return NotFound(new { success = false, message = "Data perangkat tidak ditemukan." });

        return Ok(new { success = true, message = "Data barang berhasil diarsipkan!" });
    }

    [HttpGet("arsip")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Arsip()
        => Ok(new { success = true, data = await _perangkatService.GetArsipDataAsync() });

    [HttpPost("{id}/restore")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Restore(int id)
    {
        var currentUserName = User.Identity?.Name ?? "System";
        var success = await _perangkatService.RestoreAsync(id, currentUserName);
        if (!success) return NotFound(new { success = false, message = "Data perangkat tidak ditemukan." });
        return Ok(new { success = true, message = "Perangkat berhasil dipulihkan dari arsip!" });
    }

    [HttpDelete("{id}/permanent")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> PermanentDelete(int id)
    {
        var success = await _perangkatService.PermanentDeleteAsync(id);
        if (!success) return NotFound(new { success = false, message = "Data perangkat tidak ditemukan." });
        return Ok(new { success = true, message = "Perangkat berhasil dihapus secara permanen." });
    }

    [HttpGet("validasi")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Validasi()
        => Ok(new { success = true, data = await _perangkatService.GetValidasiPendingDataAsync() });

    [HttpPost("{id}/setujui-validasi")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetujuiValidasi(int id)
    {
        var currentUserName = User.Identity?.Name ?? "Admin";
        var success = await _perangkatService.SetujuiValidasiAsync(id, currentUserName);
        if (!success) return NotFound(new { success = false, message = "Data perangkat tidak ditemukan." });
        return Ok(new { success = true, message = "Data perangkat berhasil disetujui!" });
    }

    [HttpPost("{id}/tolak-validasi")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> TolakValidasi(int id)
    {
        var success = await _perangkatService.TolakValidasiAsync(id);
        if (!success) return NotFound(new { success = false, message = "Data perangkat tidak ditemukan." });
        return Ok(new { success = true, message = "Data perangkat ditolak dan dihapus." });
    }
}
