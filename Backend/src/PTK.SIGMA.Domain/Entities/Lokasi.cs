namespace PTK.SIGMA.Domain.Entities;

public class Lokasi
{
    public int Id { get; set; }
    public string? NamaLokasi { get; set; }
    public string? Keterangan { get; set; }
    public ICollection<Perangkat>? Perangkats { get; set; }
}
