using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using PTK.SIGMA.Api.Extensions.Hubs;
using PTK.SIGMA.Domain.SigmaService;

namespace PTK.SIGMA.Api.Extensions.Notifications;

public sealed class SignalRNotificationPublisher : INotificationPublisher
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public SignalRNotificationPublisher(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task PublishAsync()
        => _hubContext.Clients.All.SendAsync("ReceiveNotification");
}
