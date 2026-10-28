using PTK.SIGMA.Utilities.Enums;

namespace PTK.SIGMA.Domain.Entities;

public class BarangMasuk
{
    public int Id { get; set; }
    public DateTime TanggalMasuk { get; set; } = DateTime.Now;
    public int PerangkatId { get; set; }
    public Perangkat? Perangkat { get; set; }
    public int Jumlah { get; set; }
    public int? VendorId { get; set; }
    public Vendor? Vendor { get; set; }
    public string? Keterangan { get; set; }
    public StatusTransaksi Status { get; set; } = StatusTransaksi.Menunggu;
}
