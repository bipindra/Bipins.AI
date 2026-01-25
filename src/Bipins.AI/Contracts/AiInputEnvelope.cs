using System.Text.Json;

namespace Bipins.AI.Core.Contracts;

/// <summary>
/// Universal input envelope for AI operations.
/// </summary>
/// <param name="TenantId">The tenant identifier.</param>
/// <param name="CorrelationId">Correlation ID for tracing.</param>
/// <param name="InputType">The type of input (e.g., "chat", "embed", "query").</param>
/// <param name="Payload">The request payload as JSON.</param>
/// <param name="Context">Additional context metadata.</param>
public record AiInputEnvelope(
    string TenantId,
    string CorrelationId,
    string InputType,
    JsonElement Payload,
    Dictionary<string, object>? Context = null);
