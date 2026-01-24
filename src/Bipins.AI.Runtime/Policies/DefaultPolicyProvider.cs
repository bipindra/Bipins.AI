using Microsoft.Extensions.Logging;

namespace Bipins.AI.Runtime.Policies;

/// <summary>
/// Default in-memory policy provider.
/// </summary>
public class DefaultPolicyProvider : IAiPolicyProvider
{
    private readonly ILogger<DefaultPolicyProvider> _logger;
    private readonly Dictionary<string, AiPolicy> _policies = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultPolicyProvider"/> class.
    /// </summary>
    public DefaultPolicyProvider(ILogger<DefaultPolicyProvider> logger)
    {
        _logger = logger;
        // Default policy for all tenants
        _policies["*"] = new AiPolicy(
            AllowedProviders: new[] { "OpenAI", "Azure", "Qdrant" },
            MaxTokens: 100000,
            AllowedTools: null,
            LoggingFlags: LoggingFlags.All,
            RedactionFlags: RedactionFlags.None);
    }

    /// <summary>
    /// Sets a policy for a tenant.
    /// </summary>
    public void SetPolicy(string tenantId, AiPolicy policy)
    {
        _policies[tenantId] = policy;
        _logger.LogInformation("Policy set for tenant {TenantId}", tenantId);
    }

    /// <inheritdoc />
    public Task<AiPolicy> GetPolicyAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        if (_policies.TryGetValue(tenantId, out var policy))
        {
            return Task.FromResult(policy);
        }

        // Return default policy
        if (_policies.TryGetValue("*", out var defaultPolicy))
        {
            return Task.FromResult(defaultPolicy);
        }

        // Fallback policy
        var fallback = new AiPolicy(
            AllowedProviders: Array.Empty<string>(),
            MaxTokens: null,
            AllowedTools: null,
            LoggingFlags: LoggingFlags.None,
            RedactionFlags: RedactionFlags.All);

        return Task.FromResult(fallback);
    }
}
