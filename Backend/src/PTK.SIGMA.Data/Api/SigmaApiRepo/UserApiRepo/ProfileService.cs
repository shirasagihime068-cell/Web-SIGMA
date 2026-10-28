using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PTK.SIGMA.Domain.SigmaService;
using PTK.SIGMA.Domain.Entities;
using PTK.SIGMA.Data.Context;

namespace PTK.SIGMA.Data.Api.SigmaApiRepo;

public sealed class ProfileService : IProfileService
{
    private readonly AppDbContext _context;
    private readonly INotificationService _notificationService;

    public ProfileService(AppDbContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<UserProfileResponseDto?> GetProfileAsync(string email)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return null;

        return new UserProfileResponseDto
        {
            Id = user.Id,
            Username = user.Username,
            NamaLengkap = user.NamaLengkap ?? string.Empty,
            Role = user.Role.ToString()
        };
    }

    public async Task<(bool Success, string Message)> UpdateProfileAsync(string email, UserProfileUpdateDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
            return (false, "Profil pengguna tidak ditemukan.");

        user.NamaLengkap = dto.NamaLengkap;

        if (!string.IsNullOrEmpty(dto.NewPassword))
        {
            var passwordHasher = new PasswordHasher<User>();
            user.Password = passwordHasher.HashPassword(user, dto.NewPassword);
        }

        await _context.SaveChangesAsync();

        await _notificationService.KirimNotifikasiAsync(
            "Mengubah",
            "Profil",
            $"Memperbarui data profil untuk user: {user.Username}");

        return (true, "Profil Anda berhasil diperbarui!");
    }
}
