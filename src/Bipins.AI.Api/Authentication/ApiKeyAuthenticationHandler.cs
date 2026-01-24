using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bipins.AI.Api.Authentication;

/// <summary>
/// API key authentication handler.
/// </summary>
public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IApiKeyValidator _apiKeyValidator;
    private readonly IAuditLogger? _auditLogger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiKeyAuthenticationHandler"/> class.
    /// </summary>
    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IApiKeyValidator apiKeyValidator,
        IAuditLogger? auditLogger = null)
        : base(options, logger, encoder)
    {
        _apiKeyValidator = apiKeyValidator;
        _auditLogger = auditLogger;
    }

    /// <inheritdoc />
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Try to get API key from header
        if (!Request.Headers.TryGetValue("X-API-Key", out var apiKeyHeader))
        {
            // Also try Authorization header with "ApiKey" scheme
            var authHeader = Request.Headers.Authorization.ToString();
            if (authHeader.StartsWith("ApiKey ", StringComparison.OrdinalIgnoreCase))
            {
                apiKeyHeader = authHeader[7..];
            }
            else
            {
                return AuthenticateResult.NoResult();
            }
        }

        var apiKey = apiKeyHeader.ToString();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return AuthenticateResult.NoResult();
        }

        // Validate API key
        var validationResult = await _apiKeyValidator.ValidateAsync(apiKey, Context.RequestAborted);
        if (!validationResult.IsValid)
        {
            _auditLogger?.LogAuthentication("unknown", "unknown", "ApiKey", false, "Invalid API key");
            return AuthenticateResult.Fail("Invalid API key");
        }

        // Build claims
        var claims = new List<Claim>
        {
            new Claim("tenantId", validationResult.TenantId),
            new Claim("apiKeyId", validationResult.ApiKeyId)
        };

        // Add roles if present
        if (validationResult.Roles != null)
        {
            foreach (var role in validationResult.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        // Log successful authentication
        _auditLogger?.LogAuthentication(validationResult.ApiKeyId, validationResult.TenantId, "ApiKey", true);

        return AuthenticateResult.Success(ticket);
    }
}

/// <summary>
/// Result of API key validation.
/// </summary>
public class ApiKeyValidationResult
{
    public bool IsValid { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string ApiKeyId { get; set; } = string.Empty;
    public IReadOnlyList<string>? Roles { get; set; }
}

/// <summary>
/// Interface for validating API keys.
/// </summary>
public interface IApiKeyValidator
{
    Task<ApiKeyValidationResult> ValidateAsync(string apiKey, CancellationToken cancellationToken = default);
}

/// <summary>
/// In-memory API key validator (for development/testing).
/// </summary>
public class InMemoryApiKeyValidator : IApiKeyValidator
{
    private readonly Dictionary<string, ApiKeyInfo> _apiKeys;
    private readonly ILogger<InMemoryApiKeyValidator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryApiKeyValidator"/> class.
    /// </summary>
    public InMemoryApiKeyValidator(ILogger<InMemoryApiKeyValidator> logger)
    {
        _logger = logger;
        _apiKeys = new Dictionary<string, ApiKeyInfo>(StringComparer.OrdinalIgnoreCase);
        
        // Add a default API key for testing (in production, load from database/configuration)
        _apiKeys["test-api-key"] = new ApiKeyInfo
        {
            TenantId = "default",
            ApiKeyId = "test-key-1",
            Roles = new[] { "User" }
        };
    }

    /// <inheritdoc />
    public Task<ApiKeyValidationResult> ValidateAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        if (_apiKeys.TryGetValue(apiKey, out var keyInfo))
        {
            return Task.FromResult(new ApiKeyValidationResult
            {
                IsValid = true,
                TenantId = keyInfo.TenantId,
                ApiKeyId = keyInfo.ApiKeyId,
                Roles = keyInfo.Roles
            });
        }

        _logger.LogWarning("Invalid API key attempted: {ApiKeyPrefix}", apiKey.Length > 8 ? apiKey.Substring(0, 8) + "..." : "***");
        return Task.FromResult(new ApiKeyValidationResult { IsValid = false });
    }

    /// <summary>
    /// Adds an API key to the validator.
    /// </summary>
    public void AddApiKey(string apiKey, string tenantId, string apiKeyId, IReadOnlyList<string>? roles = null)
    {
        _apiKeys[apiKey] = new ApiKeyInfo
        {
            TenantId = tenantId,
            ApiKeyId = apiKeyId,
            Roles = roles
        };
    }

    private class ApiKeyInfo
    {
        public string TenantId { get; set; } = string.Empty;
        public string ApiKeyId { get; set; } = string.Empty;
        public IReadOnlyList<string>? Roles { get; set; }
    }
}
