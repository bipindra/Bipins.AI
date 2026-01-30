using Bipins.AI.Core.Models;

namespace Bipins.AI.Agents.Memory;

/// <summary>
/// Interface for agent memory to store and retrieve conversation context.
/// </summary>
public interface IAgentMemory
{
    /// <summary>
    /// Saves a conversation turn to memory.
    /// </summary>
    Task SaveAsync(string agentId, string? sessionId, Message request, Message response, Dictionary<string, object>? metadata = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads conversation context for an agent session.
    /// </summary>
    Task<AgentMemoryContext> LoadContextAsync(string agentId, string? sessionId = null, int maxTurns = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches memory for relevant past conversations.
    /// </summary>
    Task<IReadOnlyList<AgentMemoryEntry>> SearchAsync(string agentId, string query, int topK = 5, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears memory for an agent session.
    /// </summary>
    Task ClearAsync(string agentId, string? sessionId = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Memory context containing conversation history.
/// </summary>
/// <param name="AgentId">Agent identifier.</param>
/// <param name="SessionId">Optional session identifier.</param>
/// <param name="ConversationHistory">List of messages in conversation order.</param>
/// <param name="Metadata">Additional metadata.</param>
public record AgentMemoryContext(
    string AgentId,
    string? SessionId,
    IReadOnlyList<Message> ConversationHistory,
    Dictionary<string, object>? Metadata = null);

/// <summary>
/// A single memory entry.
/// </summary>
/// <param name="Id">Unique identifier for the memory entry.</param>
/// <param name="AgentId">Agent identifier.</param>
/// <param name="SessionId">Optional session identifier.</param>
/// <param name="Request">The request message.</param>
/// <param name="Response">The response message.</param>
/// <param name="Timestamp">When this memory was created.</param>
/// <param name="Metadata">Additional metadata.</param>
public record AgentMemoryEntry(
    string Id,
    string AgentId,
    string? SessionId,
    Message Request,
    Message Response,
    DateTimeOffset Timestamp,
    Dictionary<string, object>? Metadata = null);
