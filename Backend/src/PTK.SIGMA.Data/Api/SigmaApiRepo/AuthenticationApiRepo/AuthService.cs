using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using PTK.SIGMA.Data.Context;
using PTK.SIGMA.Domain.Entities;
using PTK.SIGMA.Domain.SigmaService;


namespace PTK.SIGMA.Data.Api.SigmaApiRepo
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;

        public AuthService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string Message, LoginResponseDto? Data)> AuthenticateAsync(string email, string password)
        {
            // Cari user berdasarkan Email sesuai tabel database Anda
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                return (false, "Email atau password salah!", null);
            }

            // Verifikasi password menggunakan PasswordHasher bawaan ASP.NET Core
            var passwordHasher = new PasswordHasher<User>();
            var verificationResult = passwordHasher.VerifyHashedPassword(user, user.Password, password);

            if (verificationResult != PasswordVerificationResult.Success)
            {
                return (false, "Email atau password salah!", null);
            }

            // Petakan data ke Response DTO yang sudah diperbarui
            var responseDto = new LoginResponseDto
            {
                Message = "Login berhasil",
                Username = user.Username ?? string.Empty, // <-- Menambahkan data Username asli dari database
                Email = user.Email ?? string.Empty,
                NamaUser = user.NamaLengkap ?? string.Empty,
                Role = user.Role.ToString()
            };

            return (true, "Login berhasil", responseDto);
        }
    }
}
