using System.Threading.Tasks;
using PTK.SIGMA.Domain.SigmaService;

namespace PTK.SIGMA.Domain.SigmaService;

public interface IProfileService
{
    Task<UserProfileResponseDto?> GetProfileAsync(string email);
    Task<(bool Success, string Message)> UpdateProfileAsync(string email, UserProfileUpdateDto dto);
}
