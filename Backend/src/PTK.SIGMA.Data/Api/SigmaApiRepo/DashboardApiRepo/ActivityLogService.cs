using Microsoft.EntityFrameworkCore;
using PTK.SIGMA.Domain.SigmaService;
using PTK.SIGMA.Data.Context;

namespace PTK.SIGMA.Data.Api.SigmaApiRepo;

public sealed class ActivityLogService : IActivityLogService
{
    private readonly AppDbContext _context;

    public ActivityLogService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<ActivityLogResponseDto>> GetLogsAsync(int limit = 50)
    {
        limit = Math.Clamp(limit, 1, 100);

        var logs = await _context.ActivityLogs
            .AsNoTracking()
            .OrderByDescending(l => l.Waktu)
            .Take(limit)
            .ToListAsync();

        var usernames = logs
            .Where(l => !string.IsNullOrWhiteSpace(l.Username))
            .Select(l => l.Username!)
            .Distinct()
            .ToList();

        var users = await _context.Users
            .AsNoTracking()
            .Where(u => usernames.Contains(u.Username))
            .Select(u => new
            {
                u.Username,
                u.NamaLengkap,
                Role = u.Role.ToString()
            })
            .ToListAsync();

        // Database SQL Server umumnya memakai collation case-insensitive. Data
        // lama dapat berisi pasangan seperti "admin" dan "Admin"; bentuk map
        // secara toleran agar panel aktivitas tidak gagal total karena satu
        // duplikat historis.
        var userMap = users
            .GroupBy(u => u.Username, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.First(),
                StringComparer.OrdinalIgnoreCase);

        return logs.Select(log =>
        {
            var actorDisplay = log.Username ?? "System";
            string? role = null;

            if (!string.IsNullOrWhiteSpace(log.Username) &&
                userMap.TryGetValue(log.Username, out var user))
            {
                role = user.Role;

                actorDisplay =
                    string.Equals(
                        user.Role,
                        "Admin",
                        StringComparison.OrdinalIgnoreCase)
                        ? "Admin"
                        : !string.IsNullOrWhiteSpace(user.NamaLengkap)
                            ? user.NamaLengkap
                            : user.Username;
            }

            var cleanedDetail = log.Detail ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(log.Username))
            {
                cleanedDetail = cleanedDetail.Replace("Staff " + log.Username, actorDisplay);
            }

            return new ActivityLogResponseDto
            {
                Id = log.Id,
                Username = log.Username,
                ActorDisplay = actorDisplay,
                Role = role,
                Aksi = log.Aksi,
                Target = log.Target,
                Detail = cleanedDetail,
                Waktu = log.Waktu,
                IsRead = log.IsRead
            };
        }).ToList();
    }

    public async Task<int> GetUnreadCountAsync()
    {
        return await _context.ActivityLogs
            .AsNoTracking()
            .CountAsync(l => !l.IsRead);
    }

    public async Task<bool> MarkAsReadAsync(int id)
    {
        var log = await _context.ActivityLogs.FindAsync(id);

        if (log == null)
            return false;

        log.IsRead = true;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> MarkAllAsReadAsync()
    {
        var unreadLogs = await _context.ActivityLogs
            .Where(l => !l.IsRead)
            .ToListAsync();

        if (unreadLogs.Count == 0)
            return true;

        foreach (var log in unreadLogs)
        {
            log.IsRead = true;
        }

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteLogAsync(int id)
    {
        var log = await _context.ActivityLogs.FindAsync(id);

        if (log == null)
            return false;

        _context.ActivityLogs.Remove(log);

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ClearAllLogsAsync()
    {
        var logs = await _context.ActivityLogs.ToListAsync();

        if (logs.Count == 0)
            return true;

        _context.ActivityLogs.RemoveRange(logs);

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ClearReadLogsAsync()
    {
        var readLogs = await _context.ActivityLogs
            .Where(l => l.IsRead)
            .ToListAsync();

        if (readLogs.Count == 0)
            return true;

        _context.ActivityLogs.RemoveRange(readLogs);

        await _context.SaveChangesAsync();

        return true;
    }
}
