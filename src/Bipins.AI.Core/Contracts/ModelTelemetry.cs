namespace Bipins.AI.Core.Contracts;

/// <summary>
/// Telemetry information about model usage.
/// </summary>
/// <param name="ModelId">The model identifier used.</param>
/// <param name="TokensUsed">Total tokens consumed (prompt + completion).</param>
/// <param name="LatencyMs">Latency in milliseconds.</param>
/// <param name="ProviderName">The provider name (e.g., "OpenAI", "Azure").</param>
public record ModelTelemetry(
    string ModelId,
    int TokensUsed,
    long LatencyMs,
    string ProviderName);
