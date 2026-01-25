using Microsoft.Extensions.Logging;
using AICloudCostOptimizationAdvisor.Shared.Models;

namespace AICloudCostOptimizationAdvisor.Shared.Services;

/// <summary>
/// Service for calculating cloud costs from Terraform resources.
/// Uses pricing data and estimation logic for AWS, Azure, and GCP.
/// </summary>
public class CloudCostCalculatorService : ICloudCostCalculatorService
{
    private readonly ILogger<CloudCostCalculatorService> _logger;

    public CloudCostCalculatorService(ILogger<CloudCostCalculatorService> logger)
    {
        _logger = logger;
    }

    public async Task<List<CloudCost>> CalculateCostsAsync(
        List<ParsedResource> resources,
        string timePeriod = "Monthly",
        CancellationToken cancellationToken = default)
    {
        var cloudCosts = new Dictionary<string, CloudCost>();

        foreach (var resource in resources)
        {
            var provider = resource.CloudProvider;
            if (!cloudCosts.ContainsKey(provider))
            {
                cloudCosts[provider] = new CloudCost
                {
                    Provider = provider,
                    Resources = new List<ResourceCost>()
                };
            }

            var resourceCost = CalculateResourceCost(resource, timePeriod);
            cloudCosts[provider].Resources.Add(resourceCost);
        }

        // Aggregate costs
        var result = new List<CloudCost>();
        foreach (var cloudCost in cloudCosts.Values)
        {
            cloudCost.ResourceCount = cloudCost.Resources.Count;
            cloudCost.TotalMonthlyCost = cloudCost.Resources.Sum(r => r.MonthlyCost);
            cloudCost.TotalAnnualCost = cloudCost.Resources.Sum(r => r.AnnualCost);

            // Group by service
            foreach (var resource in cloudCost.Resources)
            {
                var service = GetServiceName(resource.ResourceType);
                if (!cloudCost.CostByService.ContainsKey(service))
                {
                    cloudCost.CostByService[service] = 0;
                }
                cloudCost.CostByService[service] += resource.MonthlyCost;
            }

            // Group by region
            foreach (var resource in cloudCost.Resources)
            {
                var region = resource.Region;
                if (!cloudCost.CostByRegion.ContainsKey(region))
                {
                    cloudCost.CostByRegion[region] = 0;
                }
                cloudCost.CostByRegion[region] += resource.MonthlyCost;
            }

            result.Add(cloudCost);
        }

        return await Task.FromResult(result);
    }

    private ResourceCost CalculateResourceCost(ParsedResource resource, string timePeriod)
    {
        var resourceCost = new ResourceCost
        {
            ResourceId = resource.ResourceId,
            ResourceType = resource.ResourceType,
            CloudProvider = resource.CloudProvider,
            Region = resource.Attributes.GetValueOrDefault("region")?.ToString() ?? 
                     resource.Attributes.GetValueOrDefault("location")?.ToString() ?? 
                     "us-east-1",
            Configuration = resource.Attributes.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? "")
        };

        // Calculate cost based on resource type and provider
        decimal monthlyCost = 0;

        switch (resource.CloudProvider.ToLower())
        {
            case "aws":
                monthlyCost = CalculateAWSCost(resource);
                break;
            case "azurerm":
                monthlyCost = CalculateAzureCost(resource);
                break;
            case "google":
                monthlyCost = CalculateGCPCost(resource);
                break;
        }

        resourceCost.MonthlyCost = monthlyCost;
        resourceCost.AnnualCost = monthlyCost * 12;
        resourceCost.PricingDetails = GeneratePricingDetails(resource, monthlyCost);

        return resourceCost;
    }

    private decimal CalculateAWSCost(ParsedResource resource)
    {
        // AWS cost estimation based on resource type
        var resourceType = resource.ResourceType.ToLower();

        if (resourceType.Contains("instance"))
        {
            var instanceType = resource.Attributes.GetValueOrDefault("instance_type")?.ToString() ?? "t3.micro";
            return EstimateEC2Cost(instanceType, resource.Attributes);
        }
        else if (resourceType.Contains("s3") || resourceType.Contains("bucket"))
        {
            var storage = GetStorageSize(resource.Attributes);
            return EstimateS3Cost(storage);
        }
        else if (resourceType.Contains("rds") || resourceType.Contains("db"))
        {
            var instanceClass = resource.Attributes.GetValueOrDefault("instance_class")?.ToString() ?? "db.t3.micro";
            return EstimateRDSCost(instanceClass);
        }
        else if (resourceType.Contains("lambda"))
        {
            return EstimateLambdaCost(resource.Attributes);
        }
        else if (resourceType.Contains("ebs") || resourceType.Contains("volume"))
        {
            var size = GetStorageSize(resource.Attributes);
            return EstimateEBSCost(size);
        }

        // Default estimation
        return 10.0m; // $10/month default
    }

    private decimal CalculateAzureCost(ParsedResource resource)
    {
        var resourceType = resource.ResourceType.ToLower();

        if (resourceType.Contains("virtual_machine") || resourceType.Contains("vm"))
        {
            var vmSize = resource.Attributes.GetValueOrDefault("vm_size")?.ToString() ?? "Standard_B1s";
            return EstimateAzureVMCost(vmSize);
        }
        else if (resourceType.Contains("storage_account") || resourceType.Contains("container"))
        {
            var storage = GetStorageSize(resource.Attributes);
            return EstimateAzureStorageCost(storage);
        }
        else if (resourceType.Contains("sql") || resourceType.Contains("database"))
        {
            var tier = resource.Attributes.GetValueOrDefault("tier")?.ToString() ?? "Basic";
            return EstimateAzureSQLCost(tier);
        }
        else if (resourceType.Contains("function_app") || resourceType.Contains("function"))
        {
            return EstimateAzureFunctionCost(resource.Attributes);
        }

        return 10.0m; // $10/month default
    }

    private decimal CalculateGCPCost(ParsedResource resource)
    {
        var resourceType = resource.ResourceType.ToLower();

        if (resourceType.Contains("compute_instance") || resourceType.Contains("instance"))
        {
            var machineType = resource.Attributes.GetValueOrDefault("machine_type")?.ToString() ?? "e2-micro";
            return EstimateGCPComputeCost(machineType);
        }
        else if (resourceType.Contains("storage_bucket") || resourceType.Contains("bucket"))
        {
            var storage = GetStorageSize(resource.Attributes);
            return EstimateGCPStorageCost(storage);
        }
        else if (resourceType.Contains("sql") || resourceType.Contains("database"))
        {
            var tier = resource.Attributes.GetValueOrDefault("tier")?.ToString() ?? "db-f1-micro";
            return EstimateGCPSQLCost(tier);
        }
        else if (resourceType.Contains("cloud_function") || resourceType.Contains("function"))
        {
            return EstimateGCPFunctionCost(resource.Attributes);
        }

        return 10.0m; // $10/month default
    }

    // AWS Cost Estimation Methods
    private decimal EstimateEC2Cost(string instanceType, Dictionary<string, object> attributes)
    {
        // Simplified EC2 pricing (on-demand, Linux)
        var pricing = new Dictionary<string, decimal>
        {
            { "t3.micro", 0.0104m },
            { "t3.small", 0.0208m },
            { "t3.medium", 0.0416m },
            { "t3.large", 0.0832m },
            { "m5.large", 0.192m },
            { "m5.xlarge", 0.384m },
            { "c5.large", 0.085m },
            { "c5.xlarge", 0.17m }
        };

        var hourly = pricing.GetValueOrDefault(instanceType.ToLower(), 0.05m);
        return hourly * 730; // Approximate hours per month
    }

    private decimal EstimateS3Cost(decimal storageGB)
    {
        // S3 Standard storage: ~$0.023 per GB/month
        return storageGB * 0.023m;
    }

    private decimal EstimateRDSCost(string instanceClass)
    {
        var pricing = new Dictionary<string, decimal>
        {
            { "db.t3.micro", 15.0m },
            { "db.t3.small", 30.0m },
            { "db.t3.medium", 60.0m },
            { "db.m5.large", 200.0m }
        };

        return pricing.GetValueOrDefault(instanceClass.ToLower(), 50.0m);
    }

    private decimal EstimateLambdaCost(Dictionary<string, object> attributes)
    {
        // Lambda: $0.20 per 1M requests + compute time
        // Simplified: assume 1M requests/month = $0.20 + compute
        return 5.0m; // Estimated
    }

    private decimal EstimateEBSCost(decimal sizeGB)
    {
        // EBS gp3: ~$0.08 per GB/month
        return sizeGB * 0.08m;
    }

    // Azure Cost Estimation Methods
    private decimal EstimateAzureVMCost(string vmSize)
    {
        var pricing = new Dictionary<string, decimal>
        {
            { "Standard_B1s", 10.0m },
            { "Standard_B1ms", 20.0m },
            { "Standard_B2s", 40.0m },
            { "Standard_D2s_v3", 100.0m },
            { "Standard_D4s_v3", 200.0m }
        };

        return pricing.GetValueOrDefault(vmSize, 50.0m);
    }

    private decimal EstimateAzureStorageCost(decimal storageGB)
    {
        // Azure Blob Storage LRS: ~$0.018 per GB/month
        return storageGB * 0.018m;
    }

    private decimal EstimateAzureSQLCost(string tier)
    {
        var pricing = new Dictionary<string, decimal>
        {
            { "Basic", 5.0m },
            { "S0", 15.0m },
            { "S1", 30.0m },
            { "S2", 75.0m }
        };

        return pricing.GetValueOrDefault(tier, 30.0m);
    }

    private decimal EstimateAzureFunctionCost(Dictionary<string, object> attributes)
    {
        // Azure Functions Consumption plan: pay per execution
        return 5.0m; // Estimated
    }

    // GCP Cost Estimation Methods
    private decimal EstimateGCPComputeCost(string machineType)
    {
        var pricing = new Dictionary<string, decimal>
        {
            { "e2-micro", 7.0m },
            { "e2-small", 14.0m },
            { "e2-medium", 28.0m },
            { "n1-standard-1", 25.0m },
            { "n1-standard-2", 50.0m }
        };

        return pricing.GetValueOrDefault(machineType.ToLower(), 20.0m);
    }

    private decimal EstimateGCPStorageCost(decimal storageGB)
    {
        // GCP Standard Storage: ~$0.020 per GB/month
        return storageGB * 0.020m;
    }

    private decimal EstimateGCPSQLCost(string tier)
    {
        var pricing = new Dictionary<string, decimal>
        {
            { "db-f1-micro", 7.0m },
            { "db-g1-small", 25.0m },
            { "db-n1-standard-1", 50.0m }
        };

        return pricing.GetValueOrDefault(tier.ToLower(), 30.0m);
    }

    private decimal EstimateGCPFunctionCost(Dictionary<string, object> attributes)
    {
        // GCP Cloud Functions: pay per invocation + compute
        return 5.0m; // Estimated
    }

    // Helper Methods
    private decimal GetStorageSize(Dictionary<string, object> attributes)
    {
        var size = attributes.GetValueOrDefault("size")?.ToString() ??
                   attributes.GetValueOrDefault("storage_gb")?.ToString() ??
                   attributes.GetValueOrDefault("capacity")?.ToString() ??
                   "10";

        if (decimal.TryParse(size, out var sizeValue))
        {
            return sizeValue;
        }

        // Try to parse with units (e.g., "100GB" -> 100)
        var match = System.Text.RegularExpressions.Regex.Match(size, @"(\d+)");
        if (match.Success && decimal.TryParse(match.Groups[1].Value, out sizeValue))
        {
            return sizeValue;
        }

        return 10; // Default 10 GB
    }

    private string GetServiceName(string resourceType)
    {
        if (resourceType.Contains("instance") || resourceType.Contains("vm"))
            return "Compute";
        if (resourceType.Contains("storage") || resourceType.Contains("bucket") || resourceType.Contains("volume"))
            return "Storage";
        if (resourceType.Contains("db") || resourceType.Contains("sql") || resourceType.Contains("rds"))
            return "Database";
        if (resourceType.Contains("lambda") || resourceType.Contains("function"))
            return "Serverless";
        if (resourceType.Contains("network") || resourceType.Contains("vpc") || resourceType.Contains("subnet"))
            return "Network";
        
        return "Other";
    }

    private string GeneratePricingDetails(ParsedResource resource, decimal monthlyCost)
    {
        return $"Estimated monthly cost: ${monthlyCost:F2} based on {resource.ResourceType} configuration";
    }
}
