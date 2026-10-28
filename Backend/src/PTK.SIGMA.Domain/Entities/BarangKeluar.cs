using PTK.SIGMA.Utilities.Enums;

namespace PTK.SIGMA.Domain.Entities;

public class BarangKeluar
{
    public int Id { get; set; }
    public DateTime TanggalKeluar { get; set; } = DateTime.Now;
    public int PerangkatId { get; set; }
    public Perangkat? Perangkat { get; set; }
    public int Jumlah { get; set; }
    public string? Penerima { get; set; }
    public string? Keterangan { get; set; }
    public StatusTransaksi Status { get; set; } = StatusTransaksi.Menunggu;
}
