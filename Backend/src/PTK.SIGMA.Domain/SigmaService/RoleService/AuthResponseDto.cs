namespace PTK.SIGMA.Domain.SigmaService
{
    public class LoginResponseDto
    {
        public string Message { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty; // <-- TAMBAHKAN INI
        public string Email { get; set; } = string.Empty;
        public string NamaUser { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}
