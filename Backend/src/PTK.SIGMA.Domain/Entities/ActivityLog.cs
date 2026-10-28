namespace PTK.SIGMA.Domain.Entities;

public class ActivityLog
{
    public int Id { get; set; }
    public string? Username { get; set; }
    public string? Aksi { get; set; }
    public string? Target { get; set; }
    public string? Detail { get; set; }
    public DateTime Waktu { get; set; }
    public bool IsRead { get; set; }
}
