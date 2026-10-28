using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using PTK.SIGMA.Domain.SigmaService;

namespace PTK.SIGMA.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public sealed class FilesController : ControllerBase
{
    private readonly IFileStorage _fileStorage;

    public FilesController(IFileStorage fileStorage)
    {
        _fileStorage = fileStorage;
    }

    [HttpGet("perangkat")]
    public async Task<IActionResult> GetPerangkatImage([FromQuery] string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return BadRequest(new { message = "Nama file tidak boleh kosong." });

        try
        {
            var file = await _fileStorage.GetPerangkatImageAsync(fileName);

            if (file == null)
                return NotFound(new
                {
                    message = "File foto perangkat tidak ditemukan di MinIO.",
                    targetFile = fileName
                });

            return File(file.Content, file.ContentType);
        }
        catch (Exception)
        {
            return StatusCode(500, new
            {
                message = "Terjadi kesalahan saat mengambil foto perangkat."
            });
        }
    }

    [HttpGet("opname")]
    public async Task<IActionResult> GetOpnameImage([FromQuery] string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            var rawQuery = Request.QueryString.Value?.TrimStart('?');
            if (!string.IsNullOrWhiteSpace(rawQuery))
            {
                fileName = rawQuery.StartsWith("fileName=")
                    ? rawQuery["fileName=".Length..]
                    : rawQuery;
            }
        }

        if (string.IsNullOrWhiteSpace(fileName))
            return BadRequest(new { message = "Nama file tidak boleh kosong." });

        try
        {
            var file = await _fileStorage.GetOpnameImageAsync(fileName);
            if (file == null)
                return NotFound(new { message = "File foto tidak ditemukan di MinIO.", targetFile = fileName });

            return File(file.Content, file.ContentType);
        }
        catch (System.Exception)
        {
            return StatusCode(500, new
            {
                message = "Terjadi kesalahan pada server saat mengambil foto."
            });
        }
    }

    [HttpGet("opname/{fileName}")]
    public Task<IActionResult> GetOpnameImageByName([FromRoute] string fileName)
        => GetOpnameImage(fileName);
}
