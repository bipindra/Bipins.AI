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
        
        if (result.Succeeded && result.Principal != null)
        {
            // Ensure tenantId claim exists (extract from token or use default)
            var tenantId = result.Principal.FindFirst("tenantId")?.Value 
                ?? result.Principal.FindFirst("sub")?.Value 
                ?? "default";

            // Add tenantId claim if not present
            if (result.Principal.FindFirst("tenantId") == null)
            {
                var claims = result.Principal.Claims.ToList();
                claims.Add(new Claim("tenantId", tenantId));
                
                var identity = new ClaimsIdentity(claims, result.Principal.Identity?.AuthenticationType);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, result.Ticket?.AuthenticationScheme ?? "Bearer");
                
                return AuthenticateResult.Success(ticket);
            }
        }

        return result;
    }
}
