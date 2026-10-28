namespace PTK.SIGMA.Web.Middleware;

/// <summary>
/// Protects MVC pages using the shared ASP.NET Core authentication cookie.
/// This middleware checks whether the local application session is authenticated.
/// </summary>
public sealed class BackendAuthenticationGateMiddleware
{
    private readonly RequestDelegate _next;

    public BackendAuthenticationGateMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path;

        if (path.StartsWithSegments("/Auth", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        context.Response.OnStarting(() =>
        {
            context.Response.Headers.CacheControl =
                "no-store, no-cache, must-revalidate";
            context.Response.Headers.Pragma = "no-cache";
            context.Response.Headers.Expires = "0";
            return Task.CompletedTask;
        });

        if (context.User.Identity?.IsAuthenticated != true)
        {
            RedirectToLogin(context);
            return;
        }

        await _next(context);
    }

    private static void RedirectToLogin(HttpContext context)
    {
        var returnUrl =
            context.Request.PathBase +
            context.Request.Path +
            context.Request.QueryString;

        context.Response.Redirect(
            $"/Auth/Login?returnUrl={Uri.EscapeDataString(returnUrl)}");
    }
}
