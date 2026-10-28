using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;

namespace PTK.SIGMA.Data.Extentions.Storage;

public sealed class MinioBucketInitializer
{
    private readonly IMinioClient _client;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MinioBucketInitializer> _logger;

    public MinioBucketInitializer(
        IMinioClient client,
        IConfiguration configuration,
        ILogger<MinioBucketInitializer> logger)
    {
        _client = client;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        var bucket = _configuration["MinIO:BucketName"] ?? "webgudang-storage";

        try
        {
            var exists = await _client.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(bucket));

            if (!exists)
            {
                await _client.MakeBucketAsync(
                    new MakeBucketArgs().WithBucket(bucket));
                _logger.LogInformation("MinIO bucket {Bucket} berhasil dibuat.", bucket);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "MinIO belum dapat diakses. Pastikan MinIO berjalan di {Endpoint}.",
                _configuration["MinIO:Endpoint"]);
        }
    }
}
