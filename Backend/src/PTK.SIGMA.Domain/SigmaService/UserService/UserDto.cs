using System.ComponentModel.DataAnnotations;
using PTK.SIGMA.Utilities.Enums;

namespace PTK.SIGMA.Domain.SigmaService
{
    public class UserResponseDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? NamaLengkap { get; set; }
        public string Role { get; set; } = string.Empty;
    }

    public class UserCreateDto
    {
        [Required(ErrorMessage = "Email wajib diisi!")]
        [EmailAddress(ErrorMessage = "Format email tidak valid!")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Username wajib diisi!")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password wajib diisi!")]
        public string Password { get; set; } = string.Empty;

        public string? NamaLengkap { get; set; }
        public UserRole Role { get; set; }
    }

    public class UserUpdateDto
    {
        [Required(ErrorMessage = "Email wajib diisi!")]
        [EmailAddress(ErrorMessage = "Format email tidak valid!")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Username wajib diisi!")]
        public string Username { get; set; } = string.Empty;

        public string? NewPassword { get; set; }
        public string? NamaLengkap { get; set; }
        public UserRole Role { get; set; }
    }
}
