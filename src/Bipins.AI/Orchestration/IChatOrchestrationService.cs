using System.Text.Json;
using Bipins.AI.Core.Models;
using Bipins.AI.Core.Rag;

namespace Bipins.AI.Orchestration;

public interface IChatOrchestrationService
{
    Task<ChatOrchestrationResult> ExecuteAsync(
        string tenantId,
        ChatRequest chatRequest,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<ChatOrchestrationStreamEvent> ExecuteStreamAsync(
        string tenantId,
        ChatRequest chatRequest,
        CancellationToken cancellationToken = default);
}

public record ChatOrchestrationResult(
    ChatResponse Response,
    RetrieveResult Retrieved,
    JsonElement? ParsedStructuredOutput,
    long ElapsedMilliseconds);

public record ChatOrchestrationStreamEvent(
    string EventType,
    string? Content = null,
    bool IsComplete = false,
    Usage? Usage = null,
    string? ModelId = null,
    string? FinishReason = null,
    RetrieveResult? Retrieved = null,
    JsonElement? ParsedStructuredOutput = null,
    long? ElapsedMilliseconds = null,
    string? Error = null);
