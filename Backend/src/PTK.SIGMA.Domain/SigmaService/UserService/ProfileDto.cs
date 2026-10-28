using System.ComponentModel.DataAnnotations;

namespace PTK.SIGMA.Domain.SigmaService
{
    public class UserProfileResponseDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string NamaLengkap { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class UserProfileUpdateDto
    {
        [Required(ErrorMessage = "Nama lengkap wajib diisi!")]
        public string NamaLengkap { get; set; } = string.Empty;

        public string? NewPassword { get; set; }
    }
}
