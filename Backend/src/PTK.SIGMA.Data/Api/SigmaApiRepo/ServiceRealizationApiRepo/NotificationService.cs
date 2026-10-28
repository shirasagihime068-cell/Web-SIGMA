using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PTK.SIGMA.Domain.SigmaService;
using PTK.SIGMA.Domain.Entities;
using PTK.SIGMA.Data.Context;

namespace PTK.SIGMA.Data.Api.SigmaApiRepo;

public sealed class NotificationService : INotificationService
{
    private readonly AppDbContext _context;
    private readonly INotificationPublisher _publisher;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public NotificationService(
        AppDbContext context,
        INotificationPublisher publisher,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _publisher = publisher;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task KirimNotifikasiAsync(string aksi, string target, string detail)
    {
        var currentUsername = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

        var log = new ActivityLog
        {
            Username = currentUsername,
            Aksi = aksi,
            Target = target,
            Detail = detail,
            Waktu = DateTime.Now
        };

        _context.ActivityLogs.Add(log);
        await _context.SaveChangesAsync();

        await _publisher.PublishAsync();
    }
}
