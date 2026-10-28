using System.ComponentModel.DataAnnotations;

namespace PTK.SIGMA.Domain.SigmaService
{
    public class AuthModel
    {
        [Required(ErrorMessage = "Email wajib diisi!")]
        [EmailAddress(ErrorMessage = "Format email tidak valid!")]
        public string? Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password wajib diisi!")]
        [DataType(DataType.Password)]
        public string? Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kode Captcha wajib diisi!")]
        public string? CaptchaCode { get; set; } = string.Empty; 
    }
}
