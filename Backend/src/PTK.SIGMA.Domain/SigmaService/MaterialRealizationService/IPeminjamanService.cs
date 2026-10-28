using System;
using System.Threading.Tasks;
using PTK.SIGMA.Domain.SigmaService;

namespace PTK.SIGMA.Domain.SigmaService;

public interface IPeminjamanService
{
    Task<object> GetIndexDataAsync(string? searchString, DateTime? startDate, DateTime? endDate, string? kategori, int? page);
    Task<PeminjamanResponseDto?> GetByIdAsync(int id);
    Task<object> GetCreateOptionsAsync();
    Task<byte[]> GeneratePdfReportAsync(DateTime? startDate, DateTime? endDate);
    Task<byte[]> GenerateExcelReportAsync(DateTime start, DateTime end);
    Task<string> GenerateNextNoBaAsync(DateTime? tanggalPinjam = null);
    Task<(bool Success, string Message, PeminjamanResponseDto? Data)> CreatePeminjamanAsync(PeminjamanCreateDto dto);
    Task<(bool Success, string Message)> UpdatePeminjamanAsync(int id, PeminjamanCreateDto dto);
    Task<bool> SetujuiAsync(int id);
    Task<bool> TolakAsync(int id);
    Task<bool> KembalikanAsync(int id);
    Task<bool> DeleteAsync(int id);
}
