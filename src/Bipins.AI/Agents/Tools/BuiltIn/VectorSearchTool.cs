using System.Text.Json;
using Bipins.AI.Core.Models;
using Bipins.AI.Vector;
using Microsoft.Extensions.Logging;

namespace Bipins.AI.Agents.Tools.BuiltIn;

/// <summary>
/// Tool for performing vector similarity search using the vector store.
/// </summary>
public class VectorSearchTool : IToolExecutor
{
    private readonly IVectorStore _vectorStore;
    private readonly IEmbeddingModel _embeddingModel;
    private readonly string _collectionName;
    private readonly ILogger<VectorSearchTool>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="VectorSearchTool"/> class.
    /// </summary>
    public VectorSearchTool(
        IVectorStore vectorStore,
        IEmbeddingModel embeddingModel,
        string collectionName = "documents",
        ILogger<VectorSearchTool>? logger = null)
    {
        _vectorStore = vectorStore;
        _embeddingModel = embeddingModel;
        _collectionName = collectionName;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "vector_search";

    /// <inheritdoc />
    public string Description => "Searches for similar documents or content using vector similarity search. Useful for finding relevant information from a knowledge base.";

    /// <inheritdoc />
    public JsonElement ParametersSchema => JsonSerializer.SerializeToElement(new
    {
        type = "object",
        properties = new
        {
            query = new
            {
                type = "string",
                description = "Search query to find similar content"
            },
            topK = new
            {
                type = "integer",
                description = "Number of results to return (default: 5)",
                minimum = 1,
                maximum = 100
            },
            tenantId = new
            {
                type = "string",
                description = "Optional tenant ID for multi-tenant isolation"
            },
            collectionName = new
            {
                type = "string",
                description = "Optional collection name (default: documents)"
            }
        },
        required = new[] { "query" }
    });

    /// <inheritdoc />
    public async Task<ToolExecutionResult> ExecuteAsync(ToolCall toolCall, CancellationToken cancellationToken = default)
    {
        try
        {
            if (toolCall.Arguments.ValueKind != JsonValueKind.Object)
            {
                return new ToolExecutionResult(
                    Success: false,
                    Error: "Invalid arguments format");
            }

            var query = toolCall.Arguments.TryGetProperty("query", out var queryProp)
                ? queryProp.GetString()
                : null;

            if (string.IsNullOrWhiteSpace(query))
            {
                return new ToolExecutionResult(
                    Success: false,
                    Error: "Query is required");
            }

            var topK = toolCall.Arguments.TryGetProperty("topK", out var topKProp)
                ? topKProp.GetInt32()
                : 5;

            var tenantId = toolCall.Arguments.TryGetProperty("tenantId", out var tenantProp)
                ? tenantProp.GetString() ?? "default"
                : "default";

            var collectionName = toolCall.Arguments.TryGetProperty("collectionName", out var collProp)
                ? collProp.GetString()
                : _collectionName;

            // Generate embedding
            var embeddingRequest = new EmbeddingRequest(new[] { query });
            var embeddingResponse = await _embeddingModel.EmbedAsync(embeddingRequest, cancellationToken);
            var queryVector = embeddingResponse.Vectors[0];

            // Perform vector search
            var queryRequest = new VectorQueryRequest(
                QueryVector: queryVector,
                TopK: topK,
                TenantId: tenantId,
                CollectionName: collectionName);

            var results = await _vectorStore.QueryAsync(queryRequest, cancellationToken);

            var matches = results.Matches.Select(m => new
            {
                id = m.Record.Id,
                text = m.Record.Text,
                score = m.Score,
                metadata = m.Record.Metadata
            }).ToList();

            _logger?.LogDebug("Vector search found {Count} matches for query", matches.Count);

            return new ToolExecutionResult(
                Success: true,
                Result: new { query, matches, count = matches.Count });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error performing vector search");
            return new ToolExecutionResult(
                Success: false,
                Error: ex.Message);
        }
    }
}
