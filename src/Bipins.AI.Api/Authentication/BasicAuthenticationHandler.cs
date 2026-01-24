using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bipins.AI.Api.Authentication;

/// <summary>
/// Basic authentication handler (simplified for v1).
/// </summary>
public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IAuditLogger? _auditLogger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BasicAuthenticationHandler"/> class.
    /// </summary>
    public BasicAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IAuditLogger? auditLogger = null)
        : base(options, logger, encoder)
    {
        _auditLogger = auditLogger;
    }

    /// <inheritdoc />
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Try to extract from Authorization header (Basic auth)
        var authHeader = Request.Headers.Authorization.ToString();
        string? tenantId = null;
        string? userId = null;

        if (authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var encodedCredentials = authHeader.Substring(6);
                var credentials = Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials));
                var parts = credentials.Split(':', 2);
                if (parts.Length == 2)
                {
                    userId = parts[0];
                    // In a real implementation, validate password here
                    tenantId = Request.Headers["X-Tenant-Id"].FirstOrDefault() ?? "default";
                }
            }
            catch
            {
                // Invalid Basic auth format
            }
        }

        // Fallback: extract tenantId from header
        if (string.IsNullOrEmpty(tenantId))
        {
            tenantId = Request.Headers["X-Tenant-Id"].FirstOrDefault() ?? "default";
        }

        if (string.IsNullOrEmpty(userId))
        {
            userId = $"tenant-{tenantId}";
        }

        var claims = new List<Claim>
        {
            new Claim("tenantId", tenantId),
            new Claim(ClaimTypes.NameIdentifier, userId)
        };

        // Add default role
        claims.Add(new Claim(ClaimTypes.Role, "User"));

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        // Log authentication
        _auditLogger?.LogAuthentication(userId, tenantId, "Basic", true);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
