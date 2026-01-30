using System.Text.Json;
using Bipins.AI.Core.Models;
using Bipins.AI.Vector;
using Microsoft.Extensions.Logging;

namespace Bipins.AI.Agents.Memory;

/// <summary>
/// Agent memory implementation using vector store for semantic search.
/// </summary>
public class VectorStoreAgentMemory : IAgentMemory
{
    private readonly IVectorStore _vectorStore;
    private readonly IEmbeddingModel _embeddingModel;
    private readonly string _collectionName;
    private readonly ILogger<VectorStoreAgentMemory>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="VectorStoreAgentMemory"/> class.
    /// </summary>
    public VectorStoreAgentMemory(
        IVectorStore vectorStore,
        IEmbeddingModel embeddingModel,
        string collectionName = "agent_memory",
        ILogger<VectorStoreAgentMemory>? logger = null)
    {
        _vectorStore = vectorStore;
        _embeddingModel = embeddingModel;
        _collectionName = collectionName;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SaveAsync(string agentId, string? sessionId, Message request, Message response, Dictionary<string, object>? metadata = null, CancellationToken cancellationToken = default)
    {
        var memoryId = Guid.NewGuid().ToString("N");
        var conversationText = $"{request.Role}: {request.Content}\n{response.Role}: {response.Content}";

        // Generate embedding for the conversation
        var embeddingRequest = new EmbeddingRequest(new[] { conversationText });
        var embeddingResponse = await _embeddingModel.EmbedAsync(embeddingRequest, cancellationToken);
        var vector = embeddingResponse.Vectors[0];

        // Prepare metadata
        var memoryMetadata = new Dictionary<string, object>
        {
            ["agentId"] = agentId,
            ["memoryId"] = memoryId,
            ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ["requestRole"] = request.Role.ToString(),
            ["responseRole"] = response.Role.ToString()
        };

        if (!string.IsNullOrEmpty(sessionId))
        {
            memoryMetadata["sessionId"] = sessionId;
        }

        if (metadata != null)
        {
            foreach (var kvp in metadata)
            {
                memoryMetadata[kvp.Key] = kvp.Value;
            }
        }

        // Store in vector database
        var upsertRequest = new VectorUpsertRequest(
            Records: new[]
            {
                new VectorRecord(
                    Id: memoryId,
                    Vector: vector,
                    Text: conversationText,
                    Metadata: memoryMetadata,
                    TenantId: agentId,
                    VersionId: null)
            },
            CollectionName: _collectionName);

        await _vectorStore.UpsertAsync(upsertRequest, cancellationToken);
        _logger?.LogDebug("Saved memory entry {MemoryId} for agent {AgentId}", memoryId, agentId);
    }

    /// <inheritdoc />
    public async Task<AgentMemoryContext> LoadContextAsync(string agentId, string? sessionId = null, int maxTurns = 50, CancellationToken cancellationToken = default)
    {
        // For vector store, we'll search for recent memories
        var query = $"agentId:{agentId}";
        if (!string.IsNullOrEmpty(sessionId))
        {
            query += $" sessionId:{sessionId}";
        }

        // Use a simple query to get recent memories
        var searchResults = await SearchAsync(agentId, query, maxTurns, cancellationToken);

        var conversationHistory = new List<Message>();
        foreach (var memory in searchResults.OrderBy(m => m.Timestamp))
        {
            conversationHistory.Add(memory.Request);
            conversationHistory.Add(memory.Response);
        }

        return new AgentMemoryContext(agentId, sessionId, conversationHistory);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AgentMemoryEntry>> SearchAsync(string agentId, string query, int topK = 5, CancellationToken cancellationToken = default)
    {
        // Generate embedding for the query
        var embeddingRequest = new EmbeddingRequest(new[] { query });
        var embeddingResponse = await _embeddingModel.EmbedAsync(embeddingRequest, cancellationToken);
        var queryVector = embeddingResponse.Vectors[0];

        // Build filter for agent ID
        var filter = new VectorFilterBuilder()
            .Equal("agentId", agentId)
            .Build();

        var queryRequest = new VectorQueryRequest(
            QueryVector: queryVector,
            TopK: topK,
            TenantId: agentId,
            CollectionName: _collectionName,
            Filter: filter);

        var results = await _vectorStore.QueryAsync(queryRequest, cancellationToken);

        var memories = new List<AgentMemoryEntry>();
        foreach (var match in results.Matches)
        {
            var record = match.Record;
            if (record.Metadata == null)
            {
                continue;
            }

            // Reconstruct memory entry from vector match
            var memoryId = record.Metadata.TryGetValue("memoryId", out var idObj) 
                ? idObj.ToString() 
                : record.Id;

            var sessionId = record.Metadata.TryGetValue("sessionId", out var sessionObj) 
                ? sessionObj.ToString() 
                : null;

            var timestamp = record.Metadata.TryGetValue("timestamp", out var tsObj) && tsObj is JsonElement tsElement
                ? DateTimeOffset.FromUnixTimeSeconds(tsElement.GetInt64())
                : DateTimeOffset.UtcNow;

            // Parse conversation text back to messages
            var lines = record.Text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            Message? request = null;
            Message? response = null;

            foreach (var line in lines)
            {
                var colonIndex = line.IndexOf(':');
                if (colonIndex <= 0) continue;

                var roleStr = line.Substring(0, colonIndex).Trim();
                var content = line.Substring(colonIndex + 1).Trim();

                if (Enum.TryParse<MessageRole>(roleStr, true, out var role))
                {
                    if (request == null)
                    {
                        request = new Message(role, content);
                    }
                    else if (response == null)
                    {
                        response = new Message(role, content);
                    }
                }
            }

            if (request != null && response != null)
            {
                memories.Add(new AgentMemoryEntry(
                    Id: memoryId ?? Guid.NewGuid().ToString("N"),
                    AgentId: agentId,
                    SessionId: sessionId,
                    Request: request,
                    Response: response,
                    Timestamp: timestamp,
                    Metadata: record.Metadata));
            }
        }

        return memories;
    }

    /// <inheritdoc />
    public async Task ClearAsync(string agentId, string? sessionId = null, CancellationToken cancellationToken = default)
    {
        // Build filter for agent ID and optionally session ID
        var filterBuilder = new VectorFilterBuilder()
            .Equal("agentId", agentId);

        if (!string.IsNullOrEmpty(sessionId))
        {
            filterBuilder.Equal("sessionId", sessionId);
        }

        var filter = filterBuilder.Build();

        // Query to find all memories to delete
        // Note: We need to query first to get IDs, then delete
        // For now, we'll use a simple approach - in production, you might want to use filter-based deletion if supported
        var queryRequest = new VectorQueryRequest(
            QueryVector: new ReadOnlyMemory<float>(new float[1536]), // Dummy vector, we're using filter
            TopK: 1000, // Get all
            TenantId: agentId,
            CollectionName: _collectionName,
            Filter: filter);

        var results = await _vectorStore.QueryAsync(queryRequest, cancellationToken);

        if (results.Matches.Count > 0)
        {
            var idsToDelete = results.Matches.Select(m => m.Record.Id).ToList();
            var deleteRequest = new VectorDeleteRequest(
                Ids: idsToDelete,
                CollectionName: _collectionName);

            await _vectorStore.DeleteAsync(deleteRequest, cancellationToken);
        }

        _logger?.LogDebug("Cleared {Count} memory entries for agent {AgentId}, session {SessionId}", 
            results.Matches.Count, agentId, sessionId);
    }
}
