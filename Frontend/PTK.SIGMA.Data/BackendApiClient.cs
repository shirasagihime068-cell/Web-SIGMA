using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace PTK.SIGMA.Data;

public sealed class BackendApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BackendApiClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(method, path);
        CopyRequestCookies(request);
        return await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    }

    public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, Stream? body, string? contentType, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(method, path);
        CopyRequestCookies(request);

        if (body is not null)
        {
            request.Content = new StreamContent(body);
            if (!string.IsNullOrWhiteSpace(contentType))
            {
                request.Content.Headers.TryAddWithoutValidation("Content-Type", contentType);
            }
        }

        return await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    }

    public async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string path,
        HttpContent content,
        CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(method, path);
        CopyRequestCookies(request);
        request.Content = content;

        return await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
    }

    private void CopyRequestCookies(HttpRequestMessage request)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var cookie = httpContext?.Request.Headers.Cookie.ToString();
        if (!string.IsNullOrWhiteSpace(cookie))
        {
            request.Headers.TryAddWithoutValidation("Cookie", cookie);
        }

        if (httpContext is not null)
        {
            request.Headers.TryAddWithoutValidation(
                "X-Forwarded-Host",
                httpContext.Request.Host.Value);
            request.Headers.TryAddWithoutValidation(
                "X-Forwarded-Proto",
                httpContext.Request.Scheme);
            request.Headers.TryAddWithoutValidation(
                "X-Forwarded-For",
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1");
        }
    }
}
