using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using System.Text;
using System.Text.Json;
using AICostOptimizationAdvisor.Shared.Models;

namespace AICostOptimizationAdvisor.Shared.Services;

/// <summary>
/// Service for analyzing cost data using AWS Bedrock.
/// </summary>
public class BedrockAnalysisService : IBedrockAnalysisService
{
    private readonly IAmazonBedrockRuntime _bedrockClient;
    private readonly string _defaultModelId;

    public BedrockAnalysisService(IAmazonBedrockRuntime bedrockClient, string defaultModelId = "anthropic.claude-3-sonnet-20240229-v1:0")
    {
        _bedrockClient = bedrockClient;
        _defaultModelId = defaultModelId;
    }

    public async Task<CostAnalysis> AnalyzeCostsAsync(GetCostDataResponse costData, string? modelId = null, CancellationToken cancellationToken = default)
    {
        var model = modelId ?? _defaultModelId;
        var analysisId = Guid.NewGuid().ToString();

        var prompt = BuildAnalysisPrompt(costData);
        var requestBody = BuildBedrockRequest(prompt);

        var request = new InvokeModelRequest
        {
            ModelId = model,
            Body = new MemoryStream(Encoding.UTF8.GetBytes(requestBody)),
            ContentType = "application/json",
            Accept = "application/json"
        };

        var response = await _bedrockClient.InvokeModelAsync(request, cancellationToken);

        using var reader = new StreamReader(response.Body);
        var responseJson = await reader.ReadToEndAsync(cancellationToken);

        var bedrockResponse = JsonSerializer.Deserialize<BedrockResponse>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (bedrockResponse == null || bedrockResponse.Content == null || bedrockResponse.Content.Count == 0)
        {
            throw new InvalidOperationException("Invalid response from Bedrock");
        }

        var content = bedrockResponse.Content[0].Text ?? string.Empty;
        var analysis = ParseAnalysisResponse(content, analysisId, costData);

        return analysis;
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

    private string BuildBedrockRequest(string prompt)
    {
        var request = new
        {
            anthropic_version = "bedrock-2023-05-31",
            max_tokens = 4096,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = prompt
                }
            }
        };

        return JsonSerializer.Serialize(request, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
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

    private class BedrockResponse
    {
        public List<BedrockContent> Content { get; set; } = new();
    }

    private class BedrockContent
    {
        public string? Text { get; set; }
    }

    private class AnalysisJsonResponse
    {
        public List<CostDriver>? CostDrivers { get; set; }
        public List<CostAnomaly>? Anomalies { get; set; }
        public List<OptimizationSuggestion>? Suggestions { get; set; }
        public string? Summary { get; set; }
    }
}
