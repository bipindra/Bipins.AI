using System.Collections.Concurrent;
using Bipins.AI.Core.Models;
using Microsoft.Extensions.Logging;

namespace Bipins.AI.Agents.Memory;

/// <summary>
/// In-memory implementation of agent memory (for development/testing).
/// </summary>
public class InMemoryAgentMemory : IAgentMemory
{
    private readonly ConcurrentDictionary<string, List<AgentMemoryEntry>> _memories = new();
    private readonly ILogger<InMemoryAgentMemory>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryAgentMemory"/> class.
    /// </summary>
    public InMemoryAgentMemory(ILogger<InMemoryAgentMemory>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task SaveAsync(string agentId, string? sessionId, Message request, Message response, Dictionary<string, object>? metadata = null, CancellationToken cancellationToken = default)
    {
        var key = GetMemoryKey(agentId, sessionId);
        var entry = new AgentMemoryEntry(
            Id: Guid.NewGuid().ToString("N"),
            AgentId: agentId,
            SessionId: sessionId,
            Request: request,
            Response: response,
            Timestamp: DateTimeOffset.UtcNow,
            Metadata: metadata);

        var memories = _memories.GetOrAdd(key, _ => new List<AgentMemoryEntry>());
        lock (memories)
        {
            memories.Add(entry);
        }

        _logger?.LogDebug("Saved memory entry for agent {AgentId}, session {SessionId}", agentId, sessionId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<AgentMemoryContext> LoadContextAsync(string agentId, string? sessionId = null, int maxTurns = 50, CancellationToken cancellationToken = default)
    {
        var key = GetMemoryKey(agentId, sessionId);
        if (!_memories.TryGetValue(key, out var memories))
        {
            return Task.FromResult(new AgentMemoryContext(agentId, sessionId, Array.Empty<Message>()));
        }

        List<Message> conversationHistory;
        lock (memories)
        {
            var recentMemories = memories
                .OrderBy(m => m.Timestamp)
                .TakeLast(maxTurns)
                .ToList();

            conversationHistory = new List<Message>();
            foreach (var memory in recentMemories)
            {
                conversationHistory.Add(memory.Request);
                conversationHistory.Add(memory.Response);
            }
        }

        return Task.FromResult(new AgentMemoryContext(agentId, sessionId, conversationHistory));
    }

    /// <summary>
    /// Searches memory using simple text matching (for in-memory implementation).
    /// For production, use VectorStoreAgentMemory which supports semantic search.
    /// </summary>
    public Task<IReadOnlyList<AgentMemoryEntry>> SearchAsync(string agentId, string query, int topK = 5, CancellationToken cancellationToken = default)
    {
        var results = new List<AgentMemoryEntry>();

        foreach (var (key, memories) in _memories)
        {
            if (!key.StartsWith($"{agentId}:", StringComparison.Ordinal))
            {
                continue;
            }

            lock (memories)
            {
                var matching = memories
                    .Where(m => 
                        m.Request.Content.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        m.Response.Content.Contains(query, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(m => m.Timestamp)
                    .Take(topK)
                    .ToList();

                results.AddRange(matching);
            }
        }

        return Task.FromResult<IReadOnlyList<AgentMemoryEntry>>(
            results.OrderByDescending(r => r.Timestamp).Take(topK).ToList());
    }

    /// <inheritdoc />
    public Task ClearAsync(string agentId, string? sessionId = null, CancellationToken cancellationToken = default)
    {
        var key = GetMemoryKey(agentId, sessionId);
        _memories.TryRemove(key, out _);
        _logger?.LogDebug("Cleared memory for agent {AgentId}, session {SessionId}", agentId, sessionId);
        return Task.CompletedTask;
    }

    private static string GetMemoryKey(string agentId, string? sessionId)
    {
        return string.IsNullOrEmpty(sessionId) 
            ? agentId 
            : $"{agentId}:{sessionId}";
    }
}
