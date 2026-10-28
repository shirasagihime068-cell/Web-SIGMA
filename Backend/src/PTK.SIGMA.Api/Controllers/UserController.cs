using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using PTK.SIGMA.Domain.SigmaService;

namespace PTK.SIGMA.Api.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public sealed class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        // GET: api/user
        [HttpGet]
        public async Task<IActionResult> Index(string? searchString, string? roleFilter)
        {
            var users = await _userService.GetFilteredUsersAsync(searchString, roleFilter);
            return Ok(new { success = true, data = users });
        }

        // GET: api/user/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { success = false, message = "Pengguna tidak ditemukan." });
            }

            return Ok(new { success = true, data = user });
        }

        // POST: api/user
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] UserCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Data input tidak valid", errors = ModelState });
            }

            var result = await _userService.CreateUserFromDtoAsync(dto);
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message });
        }

        // PUT: api/user/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Edit(int id, [FromBody] UserUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Data input tidak valid", errors = ModelState });
            }

            var result = await _userService.UpdateUserFromDtoAsync(id, dto);
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message });
        }

        // DELETE: api/user/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            bool success = await _userService.DeleteUserAsync(id);
            if (!success)
            {
                return NotFound(new { success = false, message = "Pengguna tidak ditemukan." });
            }

            return Ok(new { success = true, message = "Pengguna berhasil dihapus!" });
        }
    }
}
