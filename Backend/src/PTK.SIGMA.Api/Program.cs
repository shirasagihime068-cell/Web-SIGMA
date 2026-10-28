using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi;
using PTK.SIGMA.Api;
using PTK.SIGMA.Api.Extensions.Hubs;
using PTK.SIGMA.Api.Extensions.Middlewares;
using PTK.SIGMA.Api.Extensions.Notifications;
using PTK.SIGMA.Data.Extentions;
using PTK.SIGMA.Data.Extentions.Storage;
using PTK.SIGMA.Domain.SigmaService;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddData(builder.Configuration);
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/json"]);
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddSignalR();
builder.Services.AddScoped<INotificationPublisher, SignalRNotificationPublisher>();
builder.Services.AddTransient<IClaimsTransformation, LocalUserClaimsTransformation>();

// ============================================================================
// SHARED AUTH COOKIE
// Both applications share the ASP.NET Core authentication cookie.
// For multi-instance deployments, set Authentication:DataProtectionKeysPath
// to the same persistent/shared directory on every instance.
// ============================================================================
var authKeyPath = builder.Configuration["Authentication:DataProtectionKeysPath"];
if (string.IsNullOrWhiteSpace(authKeyPath))
{
    authKeyPath = Path.Combine(
        Path.GetTempPath(),
        "PTK.SIGMA",
        "auth-keys");
}

Directory.CreateDirectory(authKeyPath);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(authKeyPath))
    .SetApplicationName("PTK.SIGMA.SharedAuth");

var swaggerEnabled = builder.Configuration.GetValue<bool?>("Swagger:Enabled")
    ?? builder.Environment.IsDevelopment();

if (swaggerEnabled)
{
    builder.Services.AddEndpointsApiExplorer();

    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "PTK.SIGMA API",
            Version = "v1",
            Description = "API sistem manajemen gudang ICT PTK.SIGMA."
        });

        options.AddSecurityDefinition("CookieAuth", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            Name = ".PTK.SIGMA.Auth",
            In = ParameterLocation.Cookie,
            Description =
                "Authentication menggunakan shared ASP.NET Core Cookie Authentication."
        });

        options.AddSecurityRequirement(document =>
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("CookieAuth", document)] = []
            });
    });
}

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

var frontendOrigins = builder.Configuration
    .GetSection("Frontend:AllowedOrigins")
    .Get<string[]>() ?? [];

if (frontendOrigins.Length == 0)
{
    var frontendBaseUrl = builder.Configuration["Frontend:BaseUrl"];
    if (!string.IsNullOrWhiteSpace(frontendBaseUrl))
    {
        frontendOrigins = [frontendBaseUrl];
    }
}

if (frontendOrigins.Length == 0)
{
    throw new InvalidOperationException(
        "Konfigurasi Frontend:AllowedOrigins atau Frontend:BaseUrl wajib diisi.");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(frontendOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 8,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy("captcha", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

builder.Services.AddAuthentication(options =>
{
    // The API consumes the shared authentication cookie.
    options.DefaultAuthenticateScheme =
        CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme =
        CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.ExpireTimeSpan = TimeSpan.FromHours(2);
    options.SlidingExpiration = true;

    // MUST match PTK.SIGMA.Web exactly.
    options.Cookie.Name = ".PTK.SIGMA.Auth";
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.HttpOnly = true;

    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };

    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 50 * 1024 * 1024;
});

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 50 * 1024 * 1024;
});

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

var app = builder.Build();

app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
        context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
        context.Response.Headers.TryAdd("Referrer-Policy", "no-referrer");
        context.Response.Headers.TryAdd("Content-Security-Policy", "frame-ancestors 'none'; object-src 'none'; base-uri 'none'");
        return Task.CompletedTask;
    });

    await next();
});

if (swaggerEnabled)
{
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "PTK.SIGMA API v1");

        options.RoutePrefix = "swagger";
    });
}

using (var scope = app.Services.CreateScope())
{
    try
    {
        var initializer =
            scope.ServiceProvider.GetRequiredService<MinioBucketInitializer>();

        await initializer.InitializeAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(
            ex,
            "MinIO initialization dilewati karena service belum tersedia.");
    }
}

app.UseMiddleware<ErrorHandlerMiddleware>();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto
});

app.UseResponseCompression();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseRouting();

app.UseCors("AllowFrontend");

app.UseRateLimiter();

app.UseSession();

app.UseAuthentication();

app.UseAuthorization();

app.MapHub<NotificationHub>("/notificationHub");

app.MapControllers();

app.Run();
