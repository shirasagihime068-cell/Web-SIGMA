using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using PTK.SIGMA.Data;

namespace PTK.SIGMA.Web.Controllers;

[ApiController]
[Route("api/{**path}")]
public sealed class ApiProxyController : ControllerBase
{
    private static readonly HashSet<string> HopByHopHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Host",
        "Connection",
        "Keep-Alive",
        "Proxy-Authenticate",
        "Proxy-Authorization",
        "TE",
        "Trailer",
        "Transfer-Encoding",
        "Upgrade"
    };

    private readonly BackendApiClient _client;

    public ApiProxyController(BackendApiClient client)
    {
        _client = client;
    }

    [HttpGet]
    public Task ProxyGet(string? path, CancellationToken ct)
        => Proxy(HttpMethod.Get, path, ct);

    [HttpPost]
    public Task ProxyPost(string? path, CancellationToken ct)
        => Proxy(HttpMethod.Post, path, ct, true);

    [HttpPut]
    public Task ProxyPut(string? path, CancellationToken ct)
        => Proxy(HttpMethod.Put, path, ct, true);

    [HttpPatch]
    public Task ProxyPatch(string? path, CancellationToken ct)
        => Proxy(HttpMethod.Patch, path, ct, true);

    [HttpDelete]
    public Task ProxyDelete(string? path, CancellationToken ct)
        => Proxy(HttpMethod.Delete, path, ct);

    private async Task Proxy(
        HttpMethod method,
        string? path,
        CancellationToken ct,
        bool withBody = false)
    {
        path ??= string.Empty;

        var target = "/api/" + path.TrimStart('/');

        if (Request.QueryString.HasValue)
        {
            target += Request.QueryString.Value;
        }

        HttpResponseMessage backendResponse;

        if (withBody && Request.HasFormContentType)
        {
            backendResponse = await SendMultipartAsync(
                method,
                target,
                ct);
        }
        else if (
            withBody &&
            (Request.ContentLength is > 0 ||
             Request.Headers.ContainsKey("Transfer-Encoding")))
        {
            backendResponse = await _client.SendAsync(
                method,
                target,
                Request.Body,
                Request.ContentType,
                ct);
        }
        else
        {
            backendResponse = await _client.SendAsync(
                method,
                target,
                ct);
        }

        using (backendResponse)
        {
            Response.StatusCode = (int)backendResponse.StatusCode;
            CopyHeaders(backendResponse);

            await backendResponse.Content.CopyToAsync(
                Response.Body,
                ct);
        }
    }

    private async Task<HttpResponseMessage> SendMultipartAsync(
        HttpMethod method,
        string target,
        CancellationToken ct)
    {
        var form = await Request.ReadFormAsync(ct);
        var multipart = new MultipartFormDataContent();

        foreach (var field in form)
        {
            foreach (var value in field.Value)
            {
                multipart.Add(
                    new StringContent(value ?? string.Empty),
                    field.Key);
            }
        }

        foreach (var file in form.Files)
        {
            var fileStream = file.OpenReadStream();
            var fileContent = new StreamContent(fileStream);

            if (!string.IsNullOrWhiteSpace(file.ContentType))
            {
                fileContent.Headers.ContentType =
                    new MediaTypeHeaderValue(file.ContentType);
            }

            multipart.Add(
                fileContent,
                file.Name,
                file.FileName);
        }

        return await _client.SendAsync(
            method,
            target,
            multipart,
            ct);
    }

    private void CopyHeaders(HttpResponseMessage response)
    {
        foreach (var header in response.Headers)
        {
            if (HopByHopHeaders.Contains(header.Key))
                continue;

            if (header.Key.Equals(
                "Set-Cookie",
                StringComparison.OrdinalIgnoreCase))
            {
                foreach (var value in header.Value)
                {
                    Response.Headers.Append(
                        "Set-Cookie",
                        value);
                }

                continue;
            }

            Response.Headers.TryAdd(
                header.Key,
                header.Value.ToArray());
        }

        foreach (var header in response.Content.Headers)
        {
            if (HopByHopHeaders.Contains(header.Key))
                continue;

            Response.Headers.TryAdd(
                header.Key,
                header.Value.ToArray());
        }
    }
}
