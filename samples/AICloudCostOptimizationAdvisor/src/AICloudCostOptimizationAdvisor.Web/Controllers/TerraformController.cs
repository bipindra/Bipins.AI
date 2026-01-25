using Microsoft.AspNetCore.Mvc;
using AICloudCostOptimizationAdvisor.Shared.Services;
using AICloudCostOptimizationAdvisor.Shared.Models;

namespace AICloudCostOptimizationAdvisor.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TerraformController : ControllerBase
{
    private readonly ITerraformParserService _terraformParser;
    private readonly ILogger<TerraformController> _logger;

    public TerraformController(
        ITerraformParserService terraformParser,
        ILogger<TerraformController> logger)
    {
        _terraformParser = terraformParser;
        _logger = logger;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "No file uploaded" });
        }

        try
        {
            using var reader = new StreamReader(file.OpenReadStream());
            var content = await reader.ReadToEndAsync(cancellationToken);

            if (!await _terraformParser.ValidateTerraformAsync(content, cancellationToken))
            {
                return BadRequest(new { error = "Invalid Terraform syntax" });
            }

            var resources = await _terraformParser.ParseTerraformAsync(content, cancellationToken);

            return Ok(new
            {
                resourceCount = resources.Count,
                providers = resources.Select(r => r.CloudProvider).Distinct().ToList(),
                resources = resources.Select(r => new
                {
                    r.ResourceId,
                    r.ResourceType,
                    r.CloudProvider
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing uploaded file");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("validate")]
    public async Task<IActionResult> Validate([FromBody] TerraformInput input, CancellationToken cancellationToken = default)
    {
        try
        {
            string content = string.Empty;

            if (!string.IsNullOrEmpty(input.Content))
            {
                content = input.Content;
            }
            else if (!string.IsNullOrEmpty(input.Url))
            {
                content = await _terraformParser.FetchTerraformFromUrlAsync(input.Url, cancellationToken);
            }
            else
            {
                return BadRequest(new { error = "Either Content or Url must be provided" });
            }

            var isValid = await _terraformParser.ValidateTerraformAsync(content, cancellationToken);
            var resources = isValid ? await _terraformParser.ParseTerraformAsync(content, cancellationToken) : new List<ParsedResource>();

            return Ok(new
            {
                isValid,
                resourceCount = resources.Count,
                providers = resources.Select(r => r.CloudProvider).Distinct().ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Terraform");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
