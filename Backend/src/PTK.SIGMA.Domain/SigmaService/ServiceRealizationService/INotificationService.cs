using System.Threading.Tasks;

namespace PTK.SIGMA.Domain.SigmaService;

public interface INotificationService
{
    Task KirimNotifikasiAsync(string aksi, string target, string detail);
}
