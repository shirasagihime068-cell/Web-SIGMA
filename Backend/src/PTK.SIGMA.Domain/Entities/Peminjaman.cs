namespace PTK.SIGMA.Domain.Entities;

public class Peminjaman
{
    public int Id { get; set; }
    public string? NamaPeminjam { get; set; }
    public string? Jabatan { get; set; }
    public string? Fungsi { get; set; }
    public int PerangkatId { get; set; }
    public virtual Perangkat? Perangkat { get; set; }
    public string? SerialNumber { get; set; }
    public int Jumlah { get; set; } = 1;
    public int? LokasiId { get; set; }
    public virtual Lokasi? Lokasi { get; set; }
    public DateTime TanggalPinjam { get; set; } = DateTime.Now;
    public DateTime? TanggalKembali { get; set; }
    public string? Status { get; set; } = "Pending";
    public string? StatusPeminjaman { get; set; } = "Menunggu Verifikasi";
    public string? Keterangan { get; set; }
    public string? NoBa { get; set; }
}
