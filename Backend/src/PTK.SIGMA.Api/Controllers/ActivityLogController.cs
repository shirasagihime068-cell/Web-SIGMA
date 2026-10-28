using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using PTK.SIGMA.Domain.SigmaService;

namespace PTK.SIGMA.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public sealed class ActivityLogController : ControllerBase
    {
        private readonly IActivityLogService _activityLogService;

        public ActivityLogController(IActivityLogService activityLogService)
        {
            _activityLogService = activityLogService;
        }

        // GET: api/activitylog?limit=50
        [HttpGet]
        public async Task<IActionResult> GetLogs([FromQuery] int limit = 50)
        {
            var logs = await _activityLogService.GetLogsAsync(limit);
            return Ok(new { success = true, data = logs });
        }

        // GET: api/activitylog/unread-count
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            int count = await _activityLogService.GetUnreadCountAsync();
            return Ok(new { success = true, unreadCount = count });
        }

        // PUT: api/activitylog/{id}/read
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            bool success = await _activityLogService.MarkAsReadAsync(id);
            if (!success)
            {
                return NotFound(new { success = false, message = "Log aktivitas tidak ditemukan." });
            }

            return Ok(new { success = true, message = "Notifikasi ditandai sudah dibaca." });
        }

        // PUT: api/activitylog/read-all
        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            await _activityLogService.MarkAllAsReadAsync();
            return Ok(new { success = true, message = "Semua notifikasi ditandai sudah dibaca." });
        }

        
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteLog(int id)
        {
            bool success = await _activityLogService.DeleteLogAsync(id);
            if (!success)
            {
                return NotFound(new { success = false, message = "Log aktivitas tidak ditemukan." });
            }

            return Ok(new { success = true, message = "Notifikasi berhasil dihapus." });
        }

        // Legacy endpoint kept for API compatibility.
        [HttpDelete("clear-all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ClearAllLogs()
        {
            await _activityLogService.ClearAllLogsAsync();
            return Ok(new { success = true, message = "Semua notifikasi berhasil dibersihkan." });
        }

        // Frontend uses this endpoint so cleanup never deletes unread notifications.
        [HttpDelete("clear-read")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ClearReadLogs()
        {
            await _activityLogService.ClearReadLogsAsync();
            return Ok(new { success = true, message = "Notifikasi yang sudah dibaca berhasil dibersihkan." });
        }
    }
}
