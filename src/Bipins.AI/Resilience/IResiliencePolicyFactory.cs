namespace Bipins.AI.Resilience;

/// <summary>
/// Interface for creating resilience policies.
/// </summary>
public interface IResiliencePolicyFactory
{
    /// <summary>
    /// Creates a resilience policy from options.
    /// </summary>
    IResiliencePolicy CreatePolicy(ResilienceOptions options);
}
