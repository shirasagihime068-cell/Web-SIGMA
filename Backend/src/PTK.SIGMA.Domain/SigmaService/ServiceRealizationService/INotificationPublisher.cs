using System.Threading.Tasks;

namespace PTK.SIGMA.Domain.SigmaService;

public interface INotificationPublisher
{
    Task PublishAsync();
}
