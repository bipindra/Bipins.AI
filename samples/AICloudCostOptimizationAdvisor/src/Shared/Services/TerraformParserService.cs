using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using AICloudCostOptimizationAdvisor.Shared.Models;

namespace AICloudCostOptimizationAdvisor.Shared.Services;

/// <summary>
/// Service for parsing Terraform scripts and extracting cloud resources.
/// Uses simple regex-based parsing for HCL syntax (can be enhanced with proper HCL parser).
/// </summary>
public class TerraformParserService : ITerraformParserService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TerraformParserService> _logger;

    public TerraformParserService(
        IHttpClientFactory httpClientFactory,
        ILogger<TerraformParserService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<string> FetchTerraformFromUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);
            var response = await client.GetStringAsync(url, cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch Terraform from URL: {Url}", url);
            throw new InvalidOperationException($"Failed to fetch Terraform from URL: {url}", ex);
        }
    }

    public async Task<bool> ValidateTerraformAsync(string terraformContent, CancellationToken cancellationToken = default)
    {
        // Basic validation - check for common Terraform syntax
        if (string.IsNullOrWhiteSpace(terraformContent))
        {
            return false;
        }

        // Check for basic Terraform blocks
        var hasResource = Regex.IsMatch(terraformContent, @"\bresource\s+""", RegexOptions.IgnoreCase);
        var hasProvider = Regex.IsMatch(terraformContent, @"\bprovider\s+""", RegexOptions.IgnoreCase) ||
                         Regex.IsMatch(terraformContent, @"\bterraform\s+{", RegexOptions.IgnoreCase);

        return hasResource || hasProvider;
    }

    public async Task<List<ParsedResource>> ParseTerraformAsync(string terraformContent, CancellationToken cancellationToken = default)
    {
        var resources = new List<ParsedResource>();

        // Parse AWS resources
        var awsResources = ParseProviderResources(terraformContent, "aws", "aws_");
        resources.AddRange(awsResources);

        // Parse Azure resources
        var azureResources = ParseProviderResources(terraformContent, "azurerm", "azurerm_");
        resources.AddRange(azureResources);

        // Parse GCP resources
        var gcpResources = ParseProviderResources(terraformContent, "google", "google_");
        resources.AddRange(gcpResources);

        return await Task.FromResult(resources);
    }

    private List<ParsedResource> ParseProviderResources(string content, string providerName, string resourcePrefix)
    {
        var resources = new List<ParsedResource>();
        
        // Pattern to match resource blocks: resource "provider_type" "name" { ... }
        var resourcePattern = $@"\bresource\s+""({resourcePrefix}[^""]+)""\s+""([^""]+)""\s*\{{([^}}]*(?:\{{[^}}]*\}}[^}}]*)*)\}}";
        var matches = Regex.Matches(content, resourcePattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

        foreach (Match match in matches)
        {
            if (match.Groups.Count < 4) continue;

            var resourceType = match.Groups[1].Value;
            var resourceName = match.Groups[2].Value;
            var resourceBody = match.Groups[3].Value;

            var resource = new ParsedResource
            {
                ResourceId = $"{providerName}.{resourceType}.{resourceName}",
                ResourceType = resourceType,
                CloudProvider = providerName,
                Attributes = ParseResourceAttributes(resourceBody)
            };

            resources.Add(resource);
        }

        return resources;
    }

    private Dictionary<string, object> ParseResourceAttributes(string resourceBody)
    {
        var attributes = new Dictionary<string, object>();

        // Parse simple key-value pairs: key = value or key = "value"
        var attributePattern = @"(\w+)\s*=\s*([^\n\r]+)";
        var matches = Regex.Matches(resourceBody, attributePattern, RegexOptions.Multiline);

        foreach (Match match in matches)
        {
            if (match.Groups.Count >= 3)
            {
                var key = match.Groups[1].Value.Trim();
                var value = match.Groups[2].Value.Trim().Trim('"').Trim('\'');
                attributes[key] = value;
            }
        }

        return attributes;
    }
}
