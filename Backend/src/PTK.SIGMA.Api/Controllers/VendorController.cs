using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using PTK.SIGMA.Domain.SigmaService; 


namespace PTK.SIGMA.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public sealed class VendorController : ControllerBase
    {
        private readonly IVendorService _vendorService;

        public VendorController(IVendorService vendorService)
        {
            _vendorService = vendorService;
        }

        // GET: api/vendor
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var vendors = await _vendorService.GetVendorsAsync();
            return Ok(new { success = true, data = vendors });
        }

        // GET: api/vendor/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var vendor = await _vendorService.GetVendorByIdAsync(id);
            if (vendor == null)
            {
                return NotFound(new { success = false, message = "Vendor tidak ditemukan." });
            }

            return Ok(new { success = true, data = vendor });
        }

        // POST: api/vendor (Menggunakan DTO)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] VendorCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Data input tidak valid", errors = ModelState });
            }

            var result = await _vendorService.CreateVendorAsync(dto);
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message });
        }

        // PUT: api/vendor/{id} (Menggunakan DTO)
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [FromBody] VendorCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Data input tidak valid", errors = ModelState });
            }

            var result = await _vendorService.UpdateVendorAsync(id, dto);
            if (!result.Success)
            {
                return NotFound(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message });
        }

        // DELETE: api/vendor/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            bool success = await _vendorService.DeleteVendorAsync(id);
            if (!success)
            {
                return NotFound(new { success = false, message = "Vendor tidak ditemukan." });
            }

            return Ok(new { success = true, message = "Vendor berhasil dihapus!" });
        }
    }
}
