using System.IO;
using System.Threading.Tasks;
using PTK.SIGMA.Domain.SigmaService;

namespace PTK.SIGMA.Domain.SigmaService;

public interface IFileStorage
{
    Task<StoredFileDto?> GetOpnameImageAsync(string fileName);

    // FUNGSI BARU UNTUK MENGAMBIL FOTO PERANGKAT
    Task<StoredFileDto?> GetPerangkatImageAsync(string fileName);

    Task<string> UploadPerangkatImageAsync(Stream fileStream, string fileName, string contentType);
    Task DeletePerangkatImageAsync(string fileName);
}
