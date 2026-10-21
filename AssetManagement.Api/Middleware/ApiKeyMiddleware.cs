using System.Security.Cryptography;
using System.Text;

namespace AssetManagement.Api.Middleware;

// Every /api request must carry the shared secret in the X-Api-Key header.
// Only the UI server (and Swagger's "Authorize" button) knows the key, so
// nobody can call the API directly. Users are signed in by the UI, not here.
public class ApiKeyMiddleware
{
    public const string HeaderName = "X-Api-Key";

    private readonly RequestDelegate _next;
    private readonly byte[] _expectedKey;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _expectedKey = Encoding.UTF8.GetBytes(
            configuration["ApiKey"] ?? throw new InvalidOperationException("ApiKey is not configured."));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Swagger's own pages are not under /api, so they stay reachable
        if (context.Request.Path.StartsWithSegments("/api") && !HasValidKey(context))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { message = "Missing or invalid API key." });
            return;
        }

        await _next(context);
    }

    private bool HasValidKey(HttpContext context)
    {
        var sent = Encoding.UTF8.GetBytes(context.Request.Headers[HeaderName].ToString());

        // Fixed-time comparison, so the response time doesn't leak how much of the key matched
        return CryptographicOperations.FixedTimeEquals(sent, _expectedKey);
    }
}
