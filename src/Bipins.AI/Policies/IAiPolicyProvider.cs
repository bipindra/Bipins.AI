namespace Bipins.AI.Runtime.Policies;

/// <summary>
/// Contract for providing AI policies per tenant.
/// </summary>
public interface IAiPolicyProvider
{
    /// <summary>
    /// Gets the AI policy for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The AI policy.</returns>
    Task<AiPolicy> GetPolicyAsync(string tenantId, CancellationToken cancellationToken = default);
}
