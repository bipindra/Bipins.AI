using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using AICloudCostOptimizationAdvisor.Shared.Services;
using AICloudCostOptimizationAdvisor.Shared.Models;

namespace AICloudCostOptimizationAdvisor.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ITerraformParserService _terraformParser;
    private readonly ICloudCostCalculatorService _costCalculator;
    private readonly IAICostAnalysisService _aiAnalysis;
    private readonly IMemoryCache _cache;

    public HomeController(
        ILogger<HomeController> logger,
        ITerraformParserService terraformParser,
        ICloudCostCalculatorService costCalculator,
        IAICostAnalysisService aiAnalysis,
        IMemoryCache cache)
    {
        _logger = logger;
        _terraformParser = terraformParser;
        _costCalculator = costCalculator;
        _aiAnalysis = aiAnalysis;
        _cache = cache;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Analyze([FromBody] TerraformInput input, CancellationToken cancellationToken = default)
    {
        try
        {
            string terraformContent = string.Empty;

            // Get Terraform content from input
            if (!string.IsNullOrEmpty(input.Content))
            {
                terraformContent = input.Content;
            }
            else if (!string.IsNullOrEmpty(input.Url))
            {
                terraformContent = await _terraformParser.FetchTerraformFromUrlAsync(input.Url, cancellationToken);
            }
            else
            {
                return BadRequest(new { error = "Either Content or Url must be provided" });
            }

            // Validate Terraform
            if (!await _terraformParser.ValidateTerraformAsync(terraformContent, cancellationToken))
            {
                return BadRequest(new { error = "Invalid Terraform syntax" });
            }

            // Parse resources
            var resources = await _terraformParser.ParseTerraformAsync(terraformContent, cancellationToken);

            if (!resources.Any())
            {
                return BadRequest(new { error = "No cloud resources found in Terraform script" });
            }

            // Filter by requested providers
            if (input.CloudProviders?.Any() == true)
            {
                resources = resources.Where(r => input.CloudProviders.Contains(r.CloudProvider, StringComparer.OrdinalIgnoreCase)).ToList();
            }

            // Calculate costs
            var timePeriod = input.Options?.TimePeriod ?? "Monthly";
            var cloudCosts = await _costCalculator.CalculateCostsAsync(resources, timePeriod, cancellationToken);

            // Create analysis
            var analysis = new TerraformAnalysis
            {
                TerraformContent = terraformContent.Length > 1000 ? terraformContent.Substring(0, 1000) + "..." : terraformContent,
                CloudCosts = cloudCosts,
                ParsedResources = resources,
                TotalMonthlyCost = cloudCosts.Sum(c => c.TotalMonthlyCost),
                TotalAnnualCost = cloudCosts.Sum(c => c.TotalAnnualCost),
                Status = "Success"
            };

            // Generate AI optimizations if requested
            if (input.Options?.IncludeOptimizations == true)
            {
                try
                {
                    var includeSecurityRisks = input.Options?.IncludeSecurityRisks ?? false;
                    var includeMermaidDiagrams = input.Options?.IncludeMermaidDiagrams ?? false;
                    
                    analysis = await _aiAnalysis.AnalyzeAsync(
                        analysis, 
                        modelId: null,
                        includeSecurityRisks: includeSecurityRisks,
                        includeMermaidDiagrams: includeMermaidDiagrams,
                        cancellationToken: cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during AI analysis");
                    analysis.ErrorMessage = $"AI analysis failed: {ex.Message}";
                }
            }

            // Cache analysis
            _cache.Set(analysis.AnalysisId, analysis, TimeSpan.FromHours(24));

            return Ok(new { analysisId = analysis.AnalysisId, analysis });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing Terraform");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
