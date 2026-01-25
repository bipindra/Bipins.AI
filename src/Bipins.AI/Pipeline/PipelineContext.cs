using System.Diagnostics;

namespace Bipins.AI.Runtime.Pipeline;

/// <summary>
/// Context passed through pipeline steps.
/// </summary>
/// <param name="TenantId">The tenant identifier.</param>
/// <param name="CorrelationId">Correlation ID for tracing.</param>
/// <param name="Stopwatch">Stopwatch for timing operations.</param>
/// <param name="Tags">Additional tags for observability.</param>
/// <param name="Claims">User claims/identity information.</param>
/// <param name="Policy">AI policy for this execution.</param>
/// <param name="Activity">OpenTelemetry activity for tracing.</param>
public record PipelineContext(
    string TenantId,
    string CorrelationId,
    Stopwatch Stopwatch,
    Dictionary<string, object> Tags,
    Dictionary<string, string>? Claims = null,
    Policies.AiPolicy? Policy = null,
    Activity? Activity = null)
{
    /// <summary>
    /// Creates a new pipeline context.
    /// </summary>
    public static PipelineContext Create(string tenantId, string correlationId)
    {
        return new PipelineContext(
            tenantId,
            correlationId,
            Stopwatch.StartNew(),
            new Dictionary<string, object>(),
            null,
            null,
            null);
    }
}
