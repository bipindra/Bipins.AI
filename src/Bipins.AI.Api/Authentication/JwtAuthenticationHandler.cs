using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bipins.AI.Api.Authentication;

/// <summary>
/// JWT authentication handler.
/// </summary>
public class JwtAuthenticationHandler : JwtBearerHandler
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JwtAuthenticationHandler"/> class.
    /// </summary>
    public JwtAuthenticationHandler(
        IOptionsMonitor<JwtBearerOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    /// <inheritdoc />
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var result = await base.HandleAuthenticateAsync();
        
        if (result.Succeeded && string.IsNullOrWhiteSpace(result.Principal?.FindFirst("tenantId")?.Value))
            return AuthenticateResult.Fail("A tenantId claim is required.");

        return result;
    }
}
