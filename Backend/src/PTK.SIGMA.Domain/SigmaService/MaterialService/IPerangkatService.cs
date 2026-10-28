using System.Collections.Generic;
using System.Threading.Tasks;
using PTK.SIGMA.Domain.SigmaService;

namespace PTK.SIGMA.Domain.SigmaService;

public interface IPerangkatService
{
    Task<List<PerangkatResponseDto>> GetCetakLaporanDataAsync();
    Task<object> GetIndexDataAsync(string? searchString, string? searchCategory, int? page);
    Task<object> GetCreateOptionsAsync();
    Task<PerangkatResponseDto?> GetByIdAsync(int id);
    Task<StoredFileDto?> GetPerangkatImageAsync(string fileName);
    Task<(bool Success, string Message)> CreatePerangkatAsync(PerangkatCreateDto dto, string currentUserName);
    Task<bool> SoftDeleteAsync(int id, string currentUserName);
    Task<(bool Success, string Message)> EditPerangkatAsync(int id, PerangkatCreateDto dto, string currentUserName);
    Task<List<PerangkatResponseDto>> GetArsipDataAsync();
    Task<bool> RestoreAsync(int id, string currentUserName);
    Task<bool> PermanentDeleteAsync(int id);
    Task<List<PerangkatResponseDto>> GetValidasiPendingDataAsync();
    Task<bool> SetujuiValidasiAsync(int id, string currentUserName);
    Task<bool> TolakValidasiAsync(int id);
}
