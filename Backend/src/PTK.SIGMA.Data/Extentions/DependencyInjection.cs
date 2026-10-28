using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minio;
using QuestPDF.Infrastructure;
using PTK.SIGMA.Data.Api.SigmaApiRepo;
using PTK.SIGMA.Data.Context;
using PTK.SIGMA.Data.Dao;
using PTK.SIGMA.Data.Extentions.Storage;
using PTK.SIGMA.Domain.SigmaService;

namespace PTK.SIGMA.Data.Extentions;

public static class DependencyInjection
{
    public static IServiceCollection AddData(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' tidak ditemukan.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
                sql.MigrationsAssembly(typeof(AppDbContext).Assembly.GetName().Name)));

        var minioConfig = configuration.GetSection("MinIO");
        services.AddSingleton<IMinioClient>(_ =>
        {
            var endpoint = minioConfig["Endpoint"]
                ?? throw new InvalidOperationException(
                    "Konfigurasi MinIO:Endpoint tidak ditemukan.");

            var accessKey = minioConfig["AccessKey"] ?? string.Empty;
            var secretKey = minioConfig["SecretKey"] ?? string.Empty;
            var withSsl = minioConfig.GetValue<bool>("WithSSL");

            return new MinioClient()
                .WithEndpoint(endpoint)
                .WithCredentials(accessKey, secretKey)
                .WithSSL(withSsl)
                .Build();
        });


        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IActivityLogService, ActivityLogService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IKelolaBarangService, KelolaBarangService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IOpnameService, OpnameService>();
        services.AddScoped<IPeminjamanService, PeminjamanService>();
        services.AddScoped<IPerangkatService, PerangkatService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IVendorService, VendorService>();
        services.AddScoped<IHomeService, HomeService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IFileStorage, MinioFileStorage>();

        services.AddHostedService<OverdueNotificationService>();
        services.AddSingleton<MinioBucketInitializer>();

        QuestPDF.Settings.License = LicenseType.Community;

        return services;
    }
}