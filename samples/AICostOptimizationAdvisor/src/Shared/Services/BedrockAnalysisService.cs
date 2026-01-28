using Bipins.AI.Core.Models;
using Bipins.AI.Vector;
using System.Text.Json;
using AICostOptimizationAdvisor.Shared.Models;

namespace AICostOptimizationAdvisor.Shared.Services;

/// <summary>
/// Service for analyzing cost data using Bipins.AI platform-agnostic interfaces (IChatModel, IChatModelStreaming, IVectorStore).
/// </summary>
public class BedrockAnalysisService : IBedrockAnalysisService
{
    private readonly IChatModel _chatModel;
    private readonly IChatModelStreaming? _chatModelStreaming;
    private readonly IVectorStore? _vectorStore;
    private readonly IEmbeddingModel? _embeddingModel;
    private readonly string _defaultModelId;

    public BedrockAnalysisService(
        IChatModel chatModel,
        string defaultModelId = "anthropic.claude-3-sonnet-20240229-v1:0",
        IChatModelStreaming? chatModelStreaming = null,
        IVectorStore? vectorStore = null,
        IEmbeddingModel? embeddingModel = null)
    {
        _chatModel = chatModel;
        _chatModelStreaming = chatModelStreaming;
        _vectorStore = vectorStore;
        _embeddingModel = embeddingModel;
        _defaultModelId = defaultModelId;
    }

    public async Task<CostAnalysis> AnalyzeCostsAsync(GetCostDataResponse costData, string? modelId = null, CancellationToken cancellationToken = default)
    {
        var model = modelId ?? _defaultModelId;
        var analysisId = Guid.NewGuid().ToString();

        var prompt = BuildAnalysisPrompt(costData);

        // Use platform-agnostic ChatRequest
        var chatRequest = new ChatRequest(
            Messages: new List<Message>
            {
                new Message(MessageRole.User, prompt)
            },
            MaxTokens: 4096,
            Temperature: 0.7f,
            Metadata: new Dictionary<string, object>
            {
                { "modelId", model }
            }
        );

        var chatResponse = await _chatModel.GenerateAsync(chatRequest, cancellationToken);

        if (string.IsNullOrEmpty(chatResponse.Content))
        {
            throw new InvalidOperationException("Invalid response from AI model");
        }

        var analysis = ParseAnalysisResponse(chatResponse.Content, analysisId, costData);

        return analysis;
    }

    public async IAsyncEnumerable<CostAnalysis> AnalyzeCostsStreamAsync(GetCostDataResponse costData, string? modelId = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_chatModelStreaming == null)
        {
            throw new InvalidOperationException("Streaming is not available. IChatModelStreaming was not provided.");
        }

        var model = modelId ?? _defaultModelId;
        var analysisId = Guid.NewGuid().ToString();
        var prompt = BuildAnalysisPrompt(costData);

        // Use platform-agnostic ChatRequest for streaming
        var chatRequest = new ChatRequest(
            Messages: new List<Message>
            {
                new Message(MessageRole.User, prompt)
            },
            MaxTokens: 4096,
            Temperature: 0.7f,
            Metadata: new Dictionary<string, object>
            {
                { "modelId", model }
            }
        );

        var accumulatedContent = new System.Text.StringBuilder();
        await foreach (var chunk in _chatModelStreaming.GenerateStreamAsync(chatRequest, cancellationToken))
        {
            if (!string.IsNullOrEmpty(chunk.Content))
            {
                accumulatedContent.Append(chunk.Content);
            }

            // If this is the final chunk, parse and return the complete analysis
            if (chunk.IsComplete)
            {
                var fullContent = accumulatedContent.ToString();
                if (!string.IsNullOrEmpty(fullContent))
                {
                    var analysis = ParseAnalysisResponse(fullContent, analysisId, costData);
                    
                    // Optionally store in vector store for future RAG queries
                    if (_vectorStore != null && _embeddingModel != null)
                    {
                        await StoreAnalysisInVectorStoreAsync(analysis, cancellationToken);
                    }
                    
                    yield return analysis;
                }
            }
        }
    }

    private async Task StoreAnalysisInVectorStoreAsync(CostAnalysis analysis, CancellationToken cancellationToken)
    {
        if (_vectorStore == null || _embeddingModel == null)
        {
            return;
        }

        try
        {
            // Create a text representation of the analysis for embedding
            var analysisText = $"{analysis.Summary} " +
                string.Join(" ", analysis.CostDrivers.Select(d => $"{d.Service} {d.Description}")) +
                string.Join(" ", analysis.Suggestions.Select(s => $"{s.Category} {s.Description}"));

            // Generate embedding
            var embeddingRequest = new EmbeddingRequest(new[] { analysisText });
            var embeddingResponse = await _embeddingModel.EmbedAsync(embeddingRequest, cancellationToken);

            if (embeddingResponse.Vectors.Count > 0)
            {
                var vector = embeddingResponse.Vectors[0];
                var vectorRecord = new VectorRecord(
                    Id: analysis.AnalysisId,
                    Vector: vector,
                    Text: analysisText,
                    Metadata: new Dictionary<string, object>
                    {
                        { "analysisId", analysis.AnalysisId },
                        { "dateRange", analysis.DateRange },
                        { "totalCost", analysis.TotalCost },
                        { "createdAt", analysis.CreatedAt.ToString("O") },
                        { "summary", analysis.Summary }
                    });

                var upsertRequest = new VectorUpsertRequest(
                    Records: new[] { vectorRecord },
                    CollectionName: "cost-analyses");

                await _vectorStore.UpsertAsync(upsertRequest, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            // Log but don't fail - vector storage is optional
            Console.WriteLine($"Error storing analysis in vector store: {ex.Message}");
        }
    }

    private string BuildAnalysisPrompt(GetCostDataResponse costData)
    {
        var costDataJson = JsonSerializer.Serialize(costData, new JsonSerializerOptions { WriteIndented = true });

        return $@"You are an AWS cost optimization expert. Analyze the following AWS cost data and provide:

1. **Cost Drivers**: Identify the top 5 services/regions driving costs
2. **Anomalies**: Highlight any unusual spending patterns
3. **Optimization Suggestions**: Provide actionable recommendations with estimated savings

Cost Data:
{costDataJson}

Format your response as JSON with the following structure:
{{
  ""costDrivers"": [
    {{
      ""service"": ""service-name"",
      ""region"": ""region-name"",
      ""amount"": 123.45,
      ""percentage"": 25.5,
      ""description"": ""Description of why this is a cost driver""
    }}
  ],
  ""anomalies"": [
    {{
      ""date"": ""2024-01-15"",
      ""service"": ""service-name"",
      ""expectedAmount"": 100.00,
      ""actualAmount"": 250.00,
      ""variance"": 150.00,
      ""description"": ""Description of the anomaly""
    }}
  ],
  ""suggestions"": [
    {{
      ""category"": ""Compute"",
      ""description"": ""Actionable recommendation"",
      ""estimatedSavings"": ""$50-100/month"",
      ""priority"": ""High"",
      ""actions"": [""action1"", ""action2""],
      ""service"": ""service-name""
    }}
  ],
  ""summary"": ""Overall summary of the cost analysis""
}}";
    }

    private CostAnalysis ParseAnalysisResponse(string content, string analysisId, GetCostDataResponse costData)
    {
        // Extract JSON from markdown code blocks if present
        var jsonStart = content.IndexOf('{');
        var jsonEnd = content.LastIndexOf('}') + 1;
        
        if (jsonStart >= 0 && jsonEnd > jsonStart)
        {
            content = content.Substring(jsonStart, jsonEnd - jsonStart);
        }

        try
        {
            var analysisJson = JsonSerializer.Deserialize<AnalysisJsonResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (analysisJson == null)
            {
                throw new InvalidOperationException("Failed to parse analysis response");
            }

            return new CostAnalysis
            {
                AnalysisId = analysisId,
                DateRange = costData.DateRange,
                CreatedAt = DateTime.UtcNow,
                CostDrivers = analysisJson.CostDrivers ?? new List<CostDriver>(),
                Anomalies = analysisJson.Anomalies ?? new List<CostAnomaly>(),
                Suggestions = analysisJson.Suggestions ?? new List<OptimizationSuggestion>(),
                TotalCost = costData.TotalCost,
                Summary = analysisJson.Summary ?? string.Empty
            };
        }
        catch (JsonException)
        {
            // Fallback: create a basic analysis if JSON parsing fails
            return new CostAnalysis
            {
                AnalysisId = analysisId,
                DateRange = costData.DateRange,
                CreatedAt = DateTime.UtcNow,
                CostDrivers = new List<CostDriver>(),
                Anomalies = new List<CostAnomaly>(),
                Suggestions = new List<OptimizationSuggestion>(),
                TotalCost = costData.TotalCost,
                Summary = "Analysis completed but response format was unexpected. Please review the raw response."
            };
        }
    }

    private class AnalysisJsonResponse
    {
        public List<CostDriver>? CostDrivers { get; set; }
        public List<CostAnomaly>? Anomalies { get; set; }
        public List<OptimizationSuggestion>? Suggestions { get; set; }
        public string? Summary { get; set; }
    }
}
