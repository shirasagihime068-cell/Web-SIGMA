using System.Collections.Generic;
using System.Threading.Tasks;
using PTK.SIGMA.Domain.SigmaService;

namespace PTK.SIGMA.Domain.SigmaService;

public interface IVendorService
{
    Task<List<VendorResponseDto>> GetVendorsAsync();
    Task<VendorResponseDto?> GetVendorByIdAsync(int id);
    Task<(bool Success, string Message)> CreateVendorAsync(VendorCreateDto dto);
    Task<(bool Success, string Message)> UpdateVendorAsync(int id, VendorCreateDto dto);
    Task<bool> DeleteVendorAsync(int id);
}
