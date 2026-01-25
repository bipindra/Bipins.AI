using System.Text.Json;
using Microsoft.Extensions.Logging;
using Bipins.AI.Core.Models;
using AICloudCostOptimizationAdvisor.Shared.Models;

namespace AICloudCostOptimizationAdvisor.Shared.Services;

/// <summary>
/// Service for analyzing Terraform costs using OpenAI via Bipins.AI platform-agnostic interfaces.
/// </summary>
public class AICostAnalysisService : IAICostAnalysisService
{
    private readonly IChatModel _chatModel;
    private readonly IChatModelStreaming? _chatModelStreaming;
    private readonly string _defaultModelId;
    private readonly ILogger<AICostAnalysisService> _logger;

    public AICostAnalysisService(
        IChatModel chatModel,
        string defaultModelId = "gpt-4o-mini",
        IChatModelStreaming? chatModelStreaming = null,
        ILogger<AICostAnalysisService>? logger = null)
    {
        _chatModel = chatModel;
        _chatModelStreaming = chatModelStreaming;
        _defaultModelId = defaultModelId;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<AICostAnalysisService>.Instance;
    }

    public async Task<TerraformAnalysis> AnalyzeAsync(
        TerraformAnalysis analysis,
        string? modelId = null,
        CancellationToken cancellationToken = default)
    {
        var model = modelId ?? _defaultModelId;
        var prompt = BuildAnalysisPrompt(analysis);

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

        ParseOptimizationResponse(chatResponse.Content, analysis);

        return analysis;
    }

    public async IAsyncEnumerable<TerraformAnalysis> AnalyzeStreamAsync(
        TerraformAnalysis analysis,
        string? modelId = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_chatModelStreaming == null)
        {
            throw new InvalidOperationException("Streaming is not available. IChatModelStreaming was not provided.");
        }

        var model = modelId ?? _defaultModelId;
        var prompt = BuildAnalysisPrompt(analysis);

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

            if (chunk.IsComplete)
            {
                var fullContent = accumulatedContent.ToString();
                if (!string.IsNullOrEmpty(fullContent))
                {
                    ParseOptimizationResponse(fullContent, analysis);
                    yield return analysis;
                }
            }
        }
    }

    private string BuildAnalysisPrompt(TerraformAnalysis analysis)
    {
        var costSummary = new System.Text.StringBuilder();
        costSummary.AppendLine("## Cloud Cost Summary");
        
        foreach (var cloudCost in analysis.CloudCosts)
        {
            costSummary.AppendLine($"### {cloudCost.Provider}");
            costSummary.AppendLine($"- Total Monthly Cost: ${cloudCost.TotalMonthlyCost:F2}");
            costSummary.AppendLine($"- Total Annual Cost: ${cloudCost.TotalAnnualCost:F2}");
            costSummary.AppendLine($"- Resource Count: {cloudCost.ResourceCount}");
            
            if (cloudCost.CostByService.Any())
            {
                costSummary.AppendLine("- Cost by Service:");
                foreach (var service in cloudCost.CostByService)
                {
                    costSummary.AppendLine($"  - {service.Key}: ${service.Value:F2}/month");
                }
            }
        }

        var resourcesSummary = new System.Text.StringBuilder();
        resourcesSummary.AppendLine("## Resources");
        foreach (var resource in analysis.ParsedResources.Take(20)) // Limit to first 20 for prompt size
        {
            resourcesSummary.AppendLine($"- {resource.ResourceType} ({resource.CloudProvider}): {resource.ResourceId}");
        }

        return $@"You are a cloud cost optimization expert. Analyze the following Terraform infrastructure and cost data to provide optimization suggestions.

{costSummary}

{resourcesSummary}

Based on this infrastructure, provide optimization suggestions in the following JSON format:
{{
  ""summary"": ""Overall summary of the cost analysis and key optimization opportunities"",
  ""optimizations"": [
    {{
      ""category"": ""Compute|Storage|Network|Other"",
      ""description"": ""Detailed description of the optimization opportunity"",
      ""estimatedSavings"": ""Estimated savings (e.g., $50-100/month)"",
      ""priority"": ""High|Medium|Low"",
      ""actions"": [""action1"", ""action2""],
      ""relatedResources"": [""resource-id-1"", ""resource-id-2""],
      ""cloudProvider"": ""AWS|Azure|GCP""
    }}
  ]
}}

Focus on:
1. Right-sizing resources (instances, storage)
2. Reserved instances vs on-demand
3. Storage optimization (tiering, lifecycle policies)
4. Unused or idle resources
5. Network optimization
6. Cost-effective alternatives

Provide actionable, specific recommendations with estimated savings.";
    }

    private void ParseOptimizationResponse(string content, TerraformAnalysis analysis)
    {
        try
        {
            // Extract JSON from markdown code blocks if present
            var jsonStart = content.IndexOf('{');
            var jsonEnd = content.LastIndexOf('}') + 1;

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                content = content.Substring(jsonStart, jsonEnd - jsonStart);
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var response = JsonSerializer.Deserialize<OptimizationResponse>(content, options);

            if (response != null)
            {
                analysis.Summary = response.Summary ?? string.Empty;
                analysis.Optimizations = response.Optimizations ?? new List<OptimizationSuggestion>();
            }
            else
            {
                _logger.LogWarning("Failed to parse optimization response, using fallback");
                analysis.Summary = "Analysis completed but response format was unexpected.";
                analysis.Optimizations = new List<OptimizationSuggestion>();
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error parsing optimization response");
            analysis.Summary = "Analysis completed but response format was unexpected. Please review the raw response.";
            analysis.Optimizations = new List<OptimizationSuggestion>();
        }
    }

    private class OptimizationResponse
    {
        public string? Summary { get; set; }
        public List<OptimizationSuggestion>? Optimizations { get; set; }
    }
}
