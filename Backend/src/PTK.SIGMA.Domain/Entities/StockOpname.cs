namespace PTK.SIGMA.Domain.Entities;

public class StockOpname
{
    public int Id { get; set; }
    public int PerangkatId { get; set; }
    public virtual Perangkat? Perangkat { get; set; }
    public int JumlahSistem { get; set; } = 1;
    public int JumlahFisik { get; set; }
    public int Selisih => JumlahFisik - JumlahSistem;
    public string KondisiFisik { get; set; } = string.Empty;
    public string? Keterangan { get; set; }
    public string? FotoOpname { get; set; }
    public DateTime TanggalOpname { get; set; } = DateTime.Now;
}
