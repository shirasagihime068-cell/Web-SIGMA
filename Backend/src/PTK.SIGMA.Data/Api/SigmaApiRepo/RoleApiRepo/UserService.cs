using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PTK.SIGMA.Data.Context;
using PTK.SIGMA.Domain.Entities;
using PTK.SIGMA.Domain.SigmaService;


namespace PTK.SIGMA.Data.Api.SigmaApiRepo
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;

        public UserService(AppDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<List<UserResponseDto>> GetFilteredUsersAsync(string? searchString, string? roleFilter)
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(u => u.Username.Contains(searchString) ||
                                       (u.NamaLengkap != null && u.NamaLengkap.Contains(searchString)) ||
                                       (u.Email != null && u.Email.Contains(searchString)));
            }

            if (!string.IsNullOrEmpty(roleFilter))
            {
                query = query.Where(u => u.Role.ToString() == roleFilter);
            }

            var users = await query.ToListAsync();
            return users.Select(MapToResponseDto).ToList();
        }

        public async Task<UserResponseDto?> GetUserByIdAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            return user != null ? MapToResponseDto(user) : null;
        }

        public async Task<(bool Success, string Message)> CreateUserFromDtoAsync(UserCreateDto dto)
        {
            if (string.IsNullOrEmpty(dto.Password))
            {
                return (false, "Password wajib diisi.");
            }

            if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
            {
                return (false, "Username ini sudah digunakan oleh pengguna lain.");
            }

            if (!string.IsNullOrEmpty(dto.Email))
            {
                bool isEmailExist = await _context.Users.AnyAsync(u => u.Email == dto.Email);
                if (isEmailExist)
                {
                    return (false, "Email ini sudah terdaftar oleh pengguna lain.");
                }
            }

            try
            {
                var user = new User
                {
                    Email = dto.Email,
                    Username = dto.Username,
                    NamaLengkap = dto.NamaLengkap,
                    Role = dto.Role,
                    CreatedAt = DateTime.Now
                };

                var passwordHasher = new PasswordHasher<User>();
                user.Password = passwordHasher.HashPassword(user, dto.Password);

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                await _notificationService.KirimNotifikasiAsync(
                    "Menambah",
                    "User",
                    $"Menambahkan pengguna baru: {user.Username}"
                );

                return (true, "Pengguna baru berhasil ditambahkan!");
            }
            catch (Exception ex)
            {
                return (false, "Database Error: " + (ex.InnerException?.Message ?? ex.Message));
            }
        }

        public async Task<(bool Success, string Message)> UpdateUserFromDtoAsync(int id, UserUpdateDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return (false, "Pengguna tidak ditemukan.");
            }

            if (!string.IsNullOrEmpty(dto.Email) && user.Email != dto.Email)
            {
                bool isEmailExist = await _context.Users.AnyAsync(u => u.Email == dto.Email);
                if (isEmailExist)
                {
                    return (false, "Email ini sudah terdaftar oleh pengguna lain.");
                }
            }

            if (user.Username != dto.Username &&
                await _context.Users.AnyAsync(u => u.Id != id && u.Username == dto.Username))
            {
                return (false, "Username ini sudah digunakan oleh pengguna lain.");
            }

            user.Username = dto.Username;
            user.Email = dto.Email;
            user.NamaLengkap = dto.NamaLengkap;
            user.Role = dto.Role;

            if (!string.IsNullOrEmpty(dto.NewPassword))
            {
                var passwordHasher = new PasswordHasher<User>();
                user.Password = passwordHasher.HashPassword(user, dto.NewPassword);
            }

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            await _notificationService.KirimNotifikasiAsync(
                "Mengubah",
                "User",
                $"Memperbarui data pengguna: {user.Username}"
            );

            return (true, "Data pengguna berhasil diperbarui!");
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            await _notificationService.KirimNotifikasiAsync(
                "Menghapus",
                "User",
                $"Menghapus pengguna: {user.Username}"
            );

            return true;
        }

        // Helper private method untuk memetakan Entity User ke UserResponseDto
        private static UserResponseDto MapToResponseDto(User u)
        {
            return new UserResponseDto
            {
                Id = u.Id,
                Email = u.Email ?? string.Empty,
                Username = u.Username ?? string.Empty,
                NamaLengkap = u.NamaLengkap,
                Role = u.Role.ToString()
            };
        }
    }
}
