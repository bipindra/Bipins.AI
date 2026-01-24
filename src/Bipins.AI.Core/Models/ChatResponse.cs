namespace Bipins.AI.Core.Models;

/// <summary>
/// Response from a chat completion.
/// </summary>
/// <param name="Content">The generated text content.</param>
/// <param name="ToolCalls">List of tool calls if any.</param>
/// <param name="Usage">Token usage information.</param>
/// <param name="ModelId">The model identifier used.</param>
/// <param name="FinishReason">Reason why generation finished.</param>
/// <param name="Safety">Optional safety information.</param>
public record ChatResponse(
    string Content,
    IReadOnlyList<ToolCall>? ToolCalls = null,
    Usage? Usage = null,
    string? ModelId = null,
    string? FinishReason = null,
    SafetyInfo? Safety = null);
