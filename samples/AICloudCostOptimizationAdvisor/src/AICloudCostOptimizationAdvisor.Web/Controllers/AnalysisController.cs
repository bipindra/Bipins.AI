using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using AICloudCostOptimizationAdvisor.Shared.Models;

namespace AICloudCostOptimizationAdvisor.Web.Controllers;

[Route("Analysis")]
public class AnalysisController : Controller
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<AnalysisController> _logger;

    public AnalysisController(IMemoryCache cache, ILogger<AnalysisController> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public IActionResult Index(string id)
    {
        if (_cache.TryGetValue(id, out TerraformAnalysis? analysis) && analysis != null)
        {
            return View("Results", analysis);
        }

        return NotFound();
    }

    [HttpGet("api/{id}")]
    public IActionResult GetApi(string id)
    {
        if (_cache.TryGetValue(id, out TerraformAnalysis? analysis) && analysis != null)
        {
            return Ok(analysis);
        }

        return NotFound(new { error = "Analysis not found" });
    }

    [HttpGet("history")]
    public IActionResult History()
    {
        // For simplicity, return empty list
        // In production, this would query a database
        return View(new List<TerraformAnalysis>());
    }
}
