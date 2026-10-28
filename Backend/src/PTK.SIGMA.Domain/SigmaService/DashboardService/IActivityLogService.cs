using System.Collections.Generic;
using System.Threading.Tasks;
using PTK.SIGMA.Domain.SigmaService;

namespace PTK.SIGMA.Domain.SigmaService;

public interface IActivityLogService
{
    Task<List<ActivityLogResponseDto>> GetLogsAsync(int limit = 50);
    Task<int> GetUnreadCountAsync();
    Task<bool> MarkAsReadAsync(int id);
    Task<bool> MarkAllAsReadAsync();
    Task<bool> DeleteLogAsync(int id);
    Task<bool> ClearAllLogsAsync();
    Task<bool> ClearReadLogsAsync();
}
