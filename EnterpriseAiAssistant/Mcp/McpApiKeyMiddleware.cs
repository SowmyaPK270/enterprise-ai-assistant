using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace EnterpriseAiAssistant.Web.Mcp;

/// <summary>
/// Guards the MCP endpoint with a shared API key. The web UI authenticates
/// users via cookie/OpenID Connect, which non-browser MCP clients cannot use,
/// so the endpoint gets its own simple credential. Applied only to the MCP
/// path (see Program.cs); a no-op when no key is configured, which Program.cs
/// only allows in the Development environment.
/// </summary>
public sealed class McpApiKeyMiddleware
{
    private const string ApiKeyHeader = "X-Api-Key";
    private const string BearerPrefix = "Bearer ";

    private readonly RequestDelegate _next;
    private readonly byte[]? _expectedKeyHash;

    public McpApiKeyMiddleware(RequestDelegate next, IOptions<McpOptions> options)
    {
        _next = next;

        var key = options.Value.ApiKey;
        _expectedKeyHash = string.IsNullOrWhiteSpace(key)
            ? null
            : SHA256.HashData(Encoding.UTF8.GetBytes(key.Trim()));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (_expectedKeyHash is null)
        {
            await _next(context);
            return;
        }

        var presented = ExtractKey(context.Request);

        // Hash both sides so the comparison is fixed-length and constant-time.
        if (presented is not null &&
            CryptographicOperations.FixedTimeEquals(
                SHA256.HashData(Encoding.UTF8.GetBytes(presented)),
                _expectedKeyHash))
        {
            await _next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.Headers.WWWAuthenticate = "Bearer";
    }

    private static string? ExtractKey(HttpRequest request)
    {
        var authorization = request.Headers.Authorization.ToString();
        if (authorization.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return authorization[BearerPrefix.Length..].Trim();
        }

        var headerKey = request.Headers[ApiKeyHeader].ToString();
        return string.IsNullOrWhiteSpace(headerKey) ? null : headerKey.Trim();
    }
}
