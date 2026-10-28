using Microsoft.AspNetCore.Http;
using PTK.SIGMA.Data;
using PTK.SIGMA.Web.Configuration;

namespace PTK.SIGMA.Web.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWebConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ApiSettings>(configuration.GetSection("ApiSettings"));
        services.AddHttpContextAccessor();
        services.AddHttpClient<BackendApiClient>((sp, client) =>
        {
            var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiSettings>>().Value;
            client.BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromMinutes(5);
        })
        .ConfigurePrimaryHttpMessageHandler(sp =>
        {
            var environment = sp.GetRequiredService<IHostEnvironment>();

            return new HttpClientHandler
            {
                UseCookies = false,
                AllowAutoRedirect = false,
                ServerCertificateCustomValidationCallback = environment.IsDevelopment()
                    ? HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    : null
            };
        });

        return services;
    }
}
