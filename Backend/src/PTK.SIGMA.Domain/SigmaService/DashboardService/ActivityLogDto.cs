namespace PTK.SIGMA.Domain.SigmaService;

public class ActivityLogResponseDto
{
    public int Id { get; set; }
    public string? Username { get; set; }
    public string? ActorDisplay { get; set; }
    public string? Role { get; set; }
    public string? Aksi { get; set; }
    public string? Target { get; set; }
    public string? Detail { get; set; }
    public DateTime Waktu { get; set; }
    public bool IsRead { get; set; }
}
