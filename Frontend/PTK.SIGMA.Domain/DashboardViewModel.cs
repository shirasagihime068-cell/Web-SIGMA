namespace PTK.SIGMA.Domain;

public sealed class DashboardViewModel
{
    public int TotalUnits { get; set; }
    public int TotalCategories { get; set; }
    public int CriticalItems { get; set; }
    public int TonerUnits { get; set; }
    public int TonerItems { get; set; }
    public int TonerCritical { get; set; }
}
