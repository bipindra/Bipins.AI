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
        // No password backend is configured. Never fabricate an authenticated identity.
        return Task.FromResult(Request.Headers.Authorization.ToString()
            .StartsWith("Basic ", StringComparison.OrdinalIgnoreCase)
                ? AuthenticateResult.Fail("Basic authentication is disabled. Use a configured API key or JWT.")
                : AuthenticateResult.NoResult());
    }
}
