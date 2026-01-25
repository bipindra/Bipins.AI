using AICloudCostOptimizationAdvisor.Shared.Models;
using AICloudCostOptimizationAdvisor.Shared.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AICloudCostOptimizationAdvisor.Tests;

public class CloudCostCalculatorServiceTests
{
    private readonly ICloudCostCalculatorService _calculator;
    private readonly Mock<ILogger<CloudCostCalculatorService>> _loggerMock;

    public CloudCostCalculatorServiceTests()
    {
        _loggerMock = new Mock<ILogger<CloudCostCalculatorService>>();
        _calculator = new CloudCostCalculatorService(_loggerMock.Object);
    }

    [Fact]
    public async Task CalculateCostsAsync_AWSResources_ReturnsCosts()
    {
        // Arrange
        var resources = new List<ParsedResource>
        {
            new ParsedResource
            {
                ResourceId = "aws.instance.web",
                ResourceType = "aws_instance",
                CloudProvider = "aws",
                Attributes = new Dictionary<string, object>
                {
                    { "instance_type", "t3.large" },
                    { "region", "us-east-1" }
                }
            },
            new ParsedResource
            {
                ResourceId = "aws.s3.bucket.data",
                ResourceType = "aws_s3_bucket",
                CloudProvider = "aws",
                Attributes = new Dictionary<string, object>
                {
                    { "region", "us-east-1" }
                }
            },
            new ParsedResource
            {
                ResourceId = "aws.db.main",
                ResourceType = "aws_db_instance",
                CloudProvider = "aws",
                Attributes = new Dictionary<string, object>
                {
                    { "instance_class", "db.t3.medium" },
                    { "region", "us-east-1" }
                }
            }
        };

        // Act
        var costs = await _calculator.CalculateCostsAsync(resources, "Monthly");

        // Assert
        Assert.NotEmpty(costs);
        var awsCost = costs.FirstOrDefault(c => c.Provider == "aws");
        Assert.NotNull(awsCost);
        Assert.True(awsCost.TotalMonthlyCost > 0);
        Assert.True(awsCost.TotalAnnualCost > 0);
        Assert.Equal(3, awsCost.ResourceCount);
        Assert.NotEmpty(awsCost.Resources);
        
        // Verify individual resource costs
        var instanceCost = awsCost.Resources.FirstOrDefault(r => r.ResourceType == "aws_instance");
        Assert.NotNull(instanceCost);
        Assert.True(instanceCost.MonthlyCost > 0);
    }

    [Fact]
    public async Task CalculateCostsAsync_AzureResources_ReturnsCosts()
    {
        // Arrange
        var resources = new List<ParsedResource>
        {
            new ParsedResource
            {
                ResourceId = "azurerm.virtual_machine.web",
                ResourceType = "azurerm_virtual_machine",
                CloudProvider = "azurerm",
                Attributes = new Dictionary<string, object>
                {
                    { "vm_size", "Standard_D2s_v3" },
                    { "location", "East US" }
                }
            },
            new ParsedResource
            {
                ResourceId = "azurerm.storage_account.data",
                ResourceType = "azurerm_storage_account",
                CloudProvider = "azurerm",
                Attributes = new Dictionary<string, object>
                {
                    { "location", "East US" },
                    { "size", "100" }
                }
            },
            new ParsedResource
            {
                ResourceId = "azurerm.sql_database.main",
                ResourceType = "azurerm_sql_database",
                CloudProvider = "azurerm",
                Attributes = new Dictionary<string, object>
                {
                    { "tier", "S1" },
                    { "location", "East US" }
                }
            }
        };

        // Act
        var costs = await _calculator.CalculateCostsAsync(resources, "Monthly");

        // Assert
        Assert.NotEmpty(costs);
        var azureCost = costs.FirstOrDefault(c => c.Provider == "azurerm");
        Assert.NotNull(azureCost);
        Assert.True(azureCost.TotalMonthlyCost > 0);
        Assert.Equal(3, azureCost.ResourceCount);
    }

    [Fact]
    public async Task CalculateCostsAsync_GCPResources_ReturnsCosts()
    {
        // Arrange
        var resources = new List<ParsedResource>
        {
            new ParsedResource
            {
                ResourceId = "google.compute_instance.web",
                ResourceType = "google_compute_instance",
                CloudProvider = "google",
                Attributes = new Dictionary<string, object>
                {
                    { "machine_type", "e2-medium" },
                    { "zone", "us-central1-a" }
                }
            },
            new ParsedResource
            {
                ResourceId = "google.storage_bucket.data",
                ResourceType = "google_storage_bucket",
                CloudProvider = "google",
                Attributes = new Dictionary<string, object>
                {
                    { "location", "US" },
                    { "storage_gb", "500" }
                }
            },
            new ParsedResource
            {
                ResourceId = "google.sql_database_instance.main",
                ResourceType = "google_sql_database_instance",
                CloudProvider = "google",
                Attributes = new Dictionary<string, object>
                {
                    { "tier", "db-f1-micro" },
                    { "region", "us-central1" }
                }
            }
        };

        // Act
        var costs = await _calculator.CalculateCostsAsync(resources, "Monthly");

        // Assert
        Assert.NotEmpty(costs);
        var gcpCost = costs.FirstOrDefault(c => c.Provider == "google");
        Assert.NotNull(gcpCost);
        Assert.True(gcpCost.TotalMonthlyCost > 0);
        Assert.Equal(3, gcpCost.ResourceCount);
    }

    [Fact]
    public async Task CalculateCostsAsync_MultiCloud_ReturnsSeparateCosts()
    {
        // Arrange
        var resources = new List<ParsedResource>
        {
            new ParsedResource
            {
                ResourceId = "aws.instance.web",
                ResourceType = "aws_instance",
                CloudProvider = "aws",
                Attributes = new Dictionary<string, object> { { "instance_type", "t3.micro" } }
            },
            new ParsedResource
            {
                ResourceId = "azurerm.virtual_machine.web",
                ResourceType = "azurerm_virtual_machine",
                CloudProvider = "azurerm",
                Attributes = new Dictionary<string, object> { { "vm_size", "Standard_B1s" } }
            },
            new ParsedResource
            {
                ResourceId = "google.compute_instance.web",
                ResourceType = "google_compute_instance",
                CloudProvider = "google",
                Attributes = new Dictionary<string, object> { { "machine_type", "e2-micro" } }
            }
        };

        // Act
        var costs = await _calculator.CalculateCostsAsync(resources, "Monthly");

        // Assert
        Assert.Equal(3, costs.Count);
        Assert.Contains(costs, c => c.Provider == "aws");
        Assert.Contains(costs, c => c.Provider == "azurerm");
        Assert.Contains(costs, c => c.Provider == "google");
    }

    [Fact]
    public async Task CalculateCostsAsync_WithStorageSize_CalculatesStorageCosts()
    {
        // Arrange
        var resources = new List<ParsedResource>
        {
            new ParsedResource
            {
                ResourceId = "aws.s3.bucket.data",
                ResourceType = "aws_s3_bucket",
                CloudProvider = "aws",
                Attributes = new Dictionary<string, object>
                {
                    { "size", "1000" },
                    { "region", "us-east-1" }
                }
            }
        };

        // Act
        var costs = await _calculator.CalculateCostsAsync(resources, "Monthly");

        // Assert
        var awsCost = costs.First();
        var storageResource = awsCost.Resources.First();
        Assert.True(storageResource.MonthlyCost > 0);
        // S3 storage at ~$0.023/GB for 1000GB should be around $23/month
        Assert.True(storageResource.MonthlyCost >= 20 && storageResource.MonthlyCost <= 30);
    }

    [Fact]
    public async Task CalculateCostsAsync_AnnualPeriod_CalculatesAnnualCosts()
    {
        // Arrange
        var resources = new List<ParsedResource>
        {
            new ParsedResource
            {
                ResourceId = "aws.instance.web",
                ResourceType = "aws_instance",
                CloudProvider = "aws",
                Attributes = new Dictionary<string, object>
                {
                    { "instance_type", "t3.micro" },
                    { "region", "us-east-1" }
                }
            }
        };

        // Act
        var costs = await _calculator.CalculateCostsAsync(resources, "Annual");

        // Assert
        var awsCost = costs.First();
        Assert.True(awsCost.TotalAnnualCost > 0);
        // Annual should be approximately 12x monthly
        Assert.True(awsCost.TotalAnnualCost >= awsCost.TotalMonthlyCost * 11.5m);
        Assert.True(awsCost.TotalAnnualCost <= awsCost.TotalMonthlyCost * 12.5m);
    }

    [Fact]
    public async Task CalculateCostsAsync_EmptyResources_ReturnsEmptyList()
    {
        // Arrange
        var resources = new List<ParsedResource>();

        // Act
        var costs = await _calculator.CalculateCostsAsync(resources, "Monthly");

        // Assert
        Assert.Empty(costs);
    }
}
