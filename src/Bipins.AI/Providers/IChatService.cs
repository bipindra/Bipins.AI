using Bipins.AI.Core.Models;

namespace Bipins.AI.LLM;

public interface IChatService
{
    Task<string> ChatAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default);
    Task<ChatResponse> ChatWithToolsAsync(
        string systemPrompt,
        string userMessage,
        IReadOnlyList<ToolDefinition>? tools = null,
        CancellationToken cancellationToken = default);
    IAsyncEnumerable<ChatResponseChunk> ChatStreamAsync(
        string systemPrompt,
        string userMessage,
        CancellationToken cancellationToken = default);
    IAsyncEnumerable<ChatResponseChunk> ChatStreamWithToolsAsync(
        string systemPrompt,
        string userMessage,
        IReadOnlyList<ToolDefinition>? tools = null,
        CancellationToken cancellationToken = default);
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
}
