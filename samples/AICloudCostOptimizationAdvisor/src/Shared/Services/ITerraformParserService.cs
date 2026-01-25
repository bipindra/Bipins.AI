using AICloudCostOptimizationAdvisor.Shared.Models;

namespace AICloudCostOptimizationAdvisor.Shared.Services;

/// <summary>
/// Service interface for parsing Terraform scripts and extracting resources.
/// </summary>
public interface ITerraformParserService
{
    /// <summary>
    /// Parses Terraform content and extracts resources by cloud provider.
    /// </summary>
    Task<List<ParsedResource>> ParseTerraformAsync(string terraformContent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches Terraform content from a URL.
    /// </summary>
    Task<string> FetchTerraformFromUrlAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates Terraform syntax.
    /// </summary>
    Task<bool> ValidateTerraformAsync(string terraformContent, CancellationToken cancellationToken = default);
}
