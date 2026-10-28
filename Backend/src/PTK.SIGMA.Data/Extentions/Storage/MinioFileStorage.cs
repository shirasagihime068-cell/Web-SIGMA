using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Minio;
using Minio.DataModel.Args;
using PTK.SIGMA.Domain.SigmaService;

namespace PTK.SIGMA.Data.Extentions.Storage;

public sealed class MinioFileStorage : IFileStorage
{
    private readonly IMinioClient _minioClient;
    private readonly string _bucketName;
    private const string FolderPerangkat = "foto-perangkat/";

    public MinioFileStorage(
        IMinioClient minioClient,
        IConfiguration configuration)
    {
        _minioClient = minioClient;
        _bucketName = configuration["MinIO:BucketName"] ?? "webgudang-storage";
    }

    public async Task<StoredFileDto?> GetOpnameImageAsync(string fileName)
    {
        // Foto opname baru disimpan di folder khusus. Fallback diperlukan
        // untuk arsip lama yang pernah dipindahkan ke folder perangkat.
        return await GetImageFromFolderAsync(fileName, "opname/")
            ?? await GetImageFromFolderAsync(fileName, FolderPerangkat);
    }

    public Task<StoredFileDto?> GetPerangkatImageAsync(string fileName)
        => GetImageFromFolderAsync(fileName, FolderPerangkat);

    private async Task<StoredFileDto?> GetImageFromFolderAsync(string fileName, string folder)
    {
        var cleanFileName = NormalizeFileName(fileName);
        if (string.IsNullOrWhiteSpace(cleanFileName))
            return null;

        var objectName = folder + cleanFileName;

        try
        {
            using var stream = new MemoryStream();

            var args = new GetObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectName)
                .WithCallbackStream(s => s.CopyTo(stream));

            await _minioClient.GetObjectAsync(args);

            return new StoredFileDto
            {
                Content = stream.ToArray(),
                ContentType = GetContentType(objectName)
            };
        }
        catch
        {
            return null;
        }
    }

    public async Task<string> UploadPerangkatImageAsync(
        Stream fileStream,
        string fileName,
        string contentType)
    {
        if (fileStream is null)
            throw new ArgumentNullException(nameof(fileStream));

        var cleanFileName = NormalizeFileName(fileName);
        if (string.IsNullOrWhiteSpace(cleanFileName))
            throw new ArgumentException(
                "Nama file tidak boleh kosong.",
                nameof(fileName));

        Stream uploadStream = fileStream;
        MemoryStream? bufferedStream = null;

        try
        {
            if (uploadStream.CanSeek)
            {
                uploadStream.Position = 0;
            }
            else
            {
                bufferedStream = new MemoryStream();
                await uploadStream.CopyToAsync(bufferedStream);
                bufferedStream.Position = 0;
                uploadStream = bufferedStream;
            }

            var objectName = FolderPerangkat + cleanFileName;

            var putArgs = new PutObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectName)
                .WithStreamData(uploadStream)
                .WithObjectSize(uploadStream.Length)
                .WithContentType(
                    string.IsNullOrWhiteSpace(contentType)
                        ? "application/octet-stream"
                        : contentType);

            await _minioClient.PutObjectAsync(putArgs);

            return cleanFileName;
        }
        finally
        {
            bufferedStream?.Dispose();
        }
    }

    public async Task DeletePerangkatImageAsync(string fileName)
    {
        var cleanFileName = NormalizeFileName(fileName);
        if (string.IsNullOrWhiteSpace(cleanFileName))
            return;

        var objectName = FolderPerangkat + cleanFileName;

        var removeArgs = new RemoveObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(objectName);

        await _minioClient.RemoveObjectAsync(removeArgs);
    }

    private static string NormalizeFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return string.Empty;

        var value = Uri.UnescapeDataString(fileName)
            .Trim()
            .Replace('\\', '/');

        while (value.StartsWith("/", StringComparison.Ordinal))
            value = value[1..];

        var prefixes = new[]
        {
            "foto-perangkat/",
            "perangkat/",
            "opname/",
            "uploads/",
            "images/"
        };

        foreach (var prefix in prefixes)
        {
            if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                value = value[prefix.Length..];
                break;
            }
        }

        return Path.GetFileName(value);
    }

    private static string GetContentType(string objectName)
    {
        var extension = Path.GetExtension(objectName);

        return extension.ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            ".heic" => "image/heic",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => "application/octet-stream"
        };
    }
}
