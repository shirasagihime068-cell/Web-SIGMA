using System.Collections.Generic;
using System.Threading.Tasks;
using PTK.SIGMA.Domain.SigmaService;

namespace PTK.SIGMA.Domain.SigmaService;

public interface IUserService
{
    Task<List<UserResponseDto>> GetFilteredUsersAsync(string? searchString, string? roleFilter);
    Task<UserResponseDto?> GetUserByIdAsync(int id);
    Task<(bool Success, string Message)> CreateUserFromDtoAsync(UserCreateDto dto);
    Task<(bool Success, string Message)> UpdateUserFromDtoAsync(int id, UserUpdateDto dto);
    Task<bool> DeleteUserAsync(int id);
}
