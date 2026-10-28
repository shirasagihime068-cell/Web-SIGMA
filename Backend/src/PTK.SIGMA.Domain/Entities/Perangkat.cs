namespace PTK.SIGMA.Domain.Entities;

public class Perangkat
{
    public int Id { get; set; }
    public string Nama { get; set; } = string.Empty;
    public string Kategori { get; set; } = string.Empty;
    public string? TypeLaptop { get; set; }
    public string? ProductId { get; set; }
    public string? SnLaptop { get; set; }
    public string? StatusSewa { get; set; }
    public int? VendorId { get; set; }
    public virtual Vendor? Vendor { get; set; }
    public int? LokasiId { get; set; }
    public virtual Lokasi? Lokasi { get; set; }
    public int Jumlah { get; set; }
    public string? Keterangan { get; set; }
    public string? Kondisi { get; set; }
    public string? Dokumentasi { get; set; }
    public bool IsDeleted { get; set; }
    public string? LokasiRak { get; set; }
    public string? LokasiBarang { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsVerified { get; set; }
}
