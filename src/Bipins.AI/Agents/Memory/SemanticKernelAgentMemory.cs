using Bipins.AI.Core.Models;
using Microsoft.Extensions.Logging;

namespace Bipins.AI.Agents.Memory;

/// <summary>
/// Feature-flagged memory wrapper used to pilot SK-backed memory behavior.
/// </summary>
public sealed class SemanticKernelAgentMemory : IAgentMemory
{
    private readonly InMemoryAgentMemory _innerMemory;
    private readonly ILogger<SemanticKernelAgentMemory>? _logger;

    public SemanticKernelAgentMemory(
        InMemoryAgentMemory innerMemory,
        ILogger<SemanticKernelAgentMemory>? logger = null)
    {
        _innerMemory = innerMemory;
        _logger = logger;
    }

    public Task SaveAsync(
        string agentId,
        string? sessionId,
        Message request,
        Message response,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("SemanticKernelAgentMemory.SaveAsync for agent {AgentId}", agentId);
        return _innerMemory.SaveAsync(agentId, sessionId, request, response, metadata, cancellationToken);
    }

    public Task<AgentMemoryContext> LoadContextAsync(
        string agentId,
        string? sessionId = null,
        int maxTurns = 50,
        CancellationToken cancellationToken = default)
    {
        return _innerMemory.LoadContextAsync(agentId, sessionId, maxTurns, cancellationToken);
    }

    public Task<IReadOnlyList<AgentMemoryEntry>> SearchAsync(
        string agentId,
        string query,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        return _innerMemory.SearchAsync(agentId, query, topK, cancellationToken);
    }

    public Task ClearAsync(string agentId, string? sessionId = null, CancellationToken cancellationToken = default)
    {
        return _innerMemory.ClearAsync(agentId, sessionId, cancellationToken);
    }
}
