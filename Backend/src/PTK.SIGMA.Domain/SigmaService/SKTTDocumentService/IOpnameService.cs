using System.Threading.Tasks;
using PTK.SIGMA.Domain.SigmaService;

namespace PTK.SIGMA.Domain.SigmaService;

public interface IOpnameService
{
    Task<object> GetIndexDataAsync(string? searchString, string? lokasiRak, string? kategori, int? bulan, int? tahun);
    Task<object> GetArsipDataAsync(int? bulan, int? tahun);
    Task<object> GetReminderStatusAsync();
    Task<(bool Success, string Message)> RecordOpnameAsync(StockOpnameCreateDto dto);
    Task<bool> DeleteOpnameAsync(int id);
    Task<byte[]> GeneratePdfReportAsync(string? lokasiRak, string? kategori, int? bulan, int? tahun);
}
