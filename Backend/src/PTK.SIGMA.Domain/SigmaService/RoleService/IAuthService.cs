using System.Threading.Tasks;
using PTK.SIGMA.Domain.SigmaService;

namespace PTK.SIGMA.Domain.SigmaService;

public interface IAuthService
{
    Task<(bool Success, string Message, LoginResponseDto? Data)> AuthenticateAsync(string email, string password);
}
