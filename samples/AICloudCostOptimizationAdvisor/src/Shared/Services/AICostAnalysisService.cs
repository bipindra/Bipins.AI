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
        bool includeSecurityRisks = false,
        bool includeMermaidDiagrams = false,
        CancellationToken cancellationToken = default)
    {
        var model = modelId ?? _defaultModelId;
        var prompt = BuildAnalysisPrompt(analysis, includeSecurityRisks, includeMermaidDiagrams);

        var chatRequest = new ChatRequest(
            Messages: new List<Message>
            {
                new Message(MessageRole.User, prompt)
            },
            MaxTokens: 8192, // Increased for Mermaid diagrams
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

        ParseOptimizationResponse(chatResponse.Content, analysis, includeSecurityRisks, includeMermaidDiagrams);

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
                    // Note: Streaming doesn't support options yet, defaulting to false
                    ParseOptimizationResponse(fullContent, analysis, false, false);
                    yield return analysis;
                }
            }
        }
    }

    private string BuildAnalysisPrompt(TerraformAnalysis analysis, bool includeSecurityRisks = false, bool includeMermaidDiagrams = false)
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

        var promptBuilder = new System.Text.StringBuilder();
        promptBuilder.AppendLine("You are a cloud cost optimization and security expert. Analyze the following Terraform infrastructure and cost data.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine(costSummary.ToString());
        promptBuilder.AppendLine();
        promptBuilder.AppendLine(resourcesSummary.ToString());
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Based on this infrastructure, provide analysis in the following JSON format:");
        promptBuilder.AppendLine("{");
        promptBuilder.AppendLine("  \"summary\": \"Overall summary of the cost analysis and key optimization opportunities\",");
        promptBuilder.AppendLine("  \"optimizations\": [");
        promptBuilder.AppendLine("    {");
        promptBuilder.AppendLine("      \"category\": \"Compute|Storage|Network|Other\",");
        promptBuilder.AppendLine("      \"description\": \"Detailed description of the optimization opportunity\",");
        promptBuilder.AppendLine("      \"estimatedSavings\": \"Estimated savings (e.g., $50-100/month)\",");
        promptBuilder.AppendLine("      \"priority\": \"High|Medium|Low\",");
        promptBuilder.AppendLine("      \"actions\": [\"action1\", \"action2\"],");
        promptBuilder.AppendLine("      \"relatedResources\": [\"resource-id-1\", \"resource-id-2\"],");
        promptBuilder.AppendLine("      \"cloudProvider\": \"AWS|Azure|GCP\"");
        promptBuilder.AppendLine("    }");
        promptBuilder.AppendLine("  ]");

        if (includeSecurityRisks)
        {
            promptBuilder.AppendLine("  ,\"securityRisks\": [");
            promptBuilder.AppendLine("    {");
            promptBuilder.AppendLine("      \"severity\": \"Critical|High|Medium|Low\",");
            promptBuilder.AppendLine("      \"category\": \"Access Control|Encryption|Network|Compliance|Other\",");
            promptBuilder.AppendLine("      \"description\": \"Description of the security risk\",");
            promptBuilder.AppendLine("      \"issue\": \"Specific security issue identified\",");
            promptBuilder.AppendLine("      \"recommendations\": [\"recommendation1\", \"recommendation2\"],");
            promptBuilder.AppendLine("      \"relatedResources\": [\"resource-id-1\"],");
            promptBuilder.AppendLine("      \"cloudProvider\": \"AWS|Azure|GCP\",");
            promptBuilder.AppendLine("      \"complianceFrameworks\": [\"PCI-DSS\", \"HIPAA\"]");
            promptBuilder.AppendLine("    }");
            promptBuilder.AppendLine("  ]");
        }

        if (includeMermaidDiagrams)
        {
            promptBuilder.AppendLine("  ,\"mermaidDiagramBefore\": \"Mermaid.js diagram code for current infrastructure architecture\",");
            promptBuilder.AppendLine("  \"mermaidDiagramAfter\": \"Mermaid.js diagram code for optimized infrastructure architecture\"");
        }

        promptBuilder.AppendLine("}");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Focus on cost optimization:");
        promptBuilder.AppendLine("1. Right-sizing resources (instances, storage)");
        promptBuilder.AppendLine("2. Reserved instances vs on-demand");
        promptBuilder.AppendLine("3. Storage optimization (tiering, lifecycle policies)");
        promptBuilder.AppendLine("4. Unused or idle resources");
        promptBuilder.AppendLine("5. Network optimization");
        promptBuilder.AppendLine("6. Cost-effective alternatives");

        if (includeSecurityRisks)
        {
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Also identify security risks:");
            promptBuilder.AppendLine("1. Insecure access controls (public S3 buckets, open security groups)");
            promptBuilder.AppendLine("2. Missing encryption (data at rest, in transit)");
            promptBuilder.AppendLine("3. Network security issues (exposed ports, missing firewalls)");
            promptBuilder.AppendLine("4. Compliance violations (PCI-DSS, HIPAA, SOC 2)");
            promptBuilder.AppendLine("5. Identity and access management issues");
            promptBuilder.AppendLine("6. Logging and monitoring gaps");
        }

        if (includeMermaidDiagrams)
        {
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Generate Mermaid.js diagrams:");
            promptBuilder.AppendLine("- Use graph TD or graph LR format");
            promptBuilder.AppendLine("- Include all major resources (VMs, databases, storage, networking)");
            promptBuilder.AppendLine("- Show relationships and data flow");
            promptBuilder.AppendLine("- Use clear, descriptive labels");
            promptBuilder.AppendLine("- For 'after' diagram, show the optimized architecture with improvements highlighted");
        }

        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Provide actionable, specific recommendations with estimated savings.");

        return promptBuilder.ToString();
    }

    private void ParseOptimizationResponse(string content, TerraformAnalysis analysis, bool includeSecurityRisks = false, bool includeMermaidDiagrams = false)
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

            // Also try to extract Mermaid diagrams from markdown code blocks if not in JSON
            if (includeMermaidDiagrams && (string.IsNullOrEmpty(analysis.MermaidDiagramBefore) || string.IsNullOrEmpty(analysis.MermaidDiagramAfter)))
            {
                ExtractMermaidDiagrams(content, analysis);
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var response = JsonSerializer.Deserialize<OptimizationResponse>(content, options);

            if (response != null)
            {
                analysis.Summary = response.Summary ?? string.Empty;
                analysis.Optimizations = response.Optimizations ?? new List<OptimizationSuggestion>();
                
                if (includeSecurityRisks)
                {
                    analysis.SecurityRisks = response.SecurityRisks ?? new List<SecurityRisk>();
                }

                if (includeMermaidDiagrams)
                {
                    if (!string.IsNullOrEmpty(response.MermaidDiagramBefore))
                    {
                        analysis.MermaidDiagramBefore = response.MermaidDiagramBefore;
                    }
                    if (!string.IsNullOrEmpty(response.MermaidDiagramAfter))
                    {
                        analysis.MermaidDiagramAfter = response.MermaidDiagramAfter;
                    }
                }
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

    private void ExtractMermaidDiagrams(string content, TerraformAnalysis analysis)
    {
        // Try to extract Mermaid diagrams from markdown code blocks
        var mermaidPattern = @"```mermaid\s*\n(.*?)```";
        var matches = System.Text.RegularExpressions.Regex.Matches(content, mermaidPattern, System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        if (matches.Count >= 1 && string.IsNullOrEmpty(analysis.MermaidDiagramBefore))
        {
            analysis.MermaidDiagramBefore = matches[0].Groups[1].Value.Trim();
        }
        
        if (matches.Count >= 2 && string.IsNullOrEmpty(analysis.MermaidDiagramAfter))
        {
            analysis.MermaidDiagramAfter = matches[1].Groups[1].Value.Trim();
        }
    }

    private class OptimizationResponse
    {
        public string? Summary { get; set; }
        public List<OptimizationSuggestion>? Optimizations { get; set; }
        public List<SecurityRisk>? SecurityRisks { get; set; }
        public string? MermaidDiagramBefore { get; set; }
        public string? MermaidDiagramAfter { get; set; }
    }
}
