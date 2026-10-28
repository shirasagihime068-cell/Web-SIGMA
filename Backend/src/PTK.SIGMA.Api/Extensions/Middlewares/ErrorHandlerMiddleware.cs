using System.Net;
using System.Text.Json;

namespace PTK.SIGMA.Api.Extensions.Middlewares;

public class ErrorHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _env;

    public ErrorHandlerMiddleware(RequestDelegate next, IWebHostEnvironment env)
    {
        _next = next;
        _env = env;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            // Melanjutkan request ke middleware / controller berikutnya
            await _next(context);
        }
        catch (Exception error)
        {
            var response = context.Response;
            response.ContentType = "application/json";

            // Menentukan status code HTTP berdasarkan jenis exception yang terjadi
            response.StatusCode = error switch
            {
                KeyNotFoundException => (int)HttpStatusCode.NotFound, // 404
                UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized, // 401
                _ => (int)HttpStatusCode.InternalServerError // Default 500 untuk error tak terduga
            };

            // Membentuk struktur respons JSON yang seragam
            var responseModel = new 
            {
                success = false,
                message = error.Message,
                // Stack trace hanya dimunculkan saat mode Development untuk keperluan debugging
                detail = _env.IsDevelopment() ? error.StackTrace?.ToString() : null
            };

            var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var result = JsonSerializer.Serialize(responseModel, jsonOptions);

            // Mengirimkan respons balik berupa JSON ke klien (Thunder Client / Frontend)
            await response.WriteAsync(result);
        }
    }
}
