using AICloudCostOptimizationAdvisor.Shared.Models;
using AICloudCostOptimizationAdvisor.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AICloudCostOptimizationAdvisor.Tests;

/// <summary>
/// Integration tests that test the full flow: Parse -> Calculate Costs
/// </summary>
public class IntegrationTests
{
    private readonly ITerraformParserService _parser;
    private readonly ICloudCostCalculatorService _calculator;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Mock<ILogger<TerraformParserService>> _parserLoggerMock;
    private readonly Mock<ILogger<CloudCostCalculatorService>> _calculatorLoggerMock;

    public IntegrationTests()
    {
        _parserLoggerMock = new Mock<ILogger<TerraformParserService>>();
        _calculatorLoggerMock = new Mock<ILogger<CloudCostCalculatorService>>();
        
        // Use real HttpClientFactory for testing
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddHttpClient();
        var serviceProvider = services.BuildServiceProvider();
        _httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        
        _parser = new TerraformParserService(_httpClientFactory, _parserLoggerMock.Object);
        _calculator = new CloudCostCalculatorService(_calculatorLoggerMock.Object);
    }

    [Fact]
    public async Task ParseAndCalculate_AWSExample_ReturnsValidCosts()
    {
        // Arrange
        var terraformContent = await File.ReadAllTextAsync("TestData/aws-example.tf");

        // Act - Parse
        var resources = await _parser.ParseTerraformAsync(terraformContent);
        
        // Act - Calculate
        var costs = await _calculator.CalculateCostsAsync(resources, "Monthly");

        // Assert
        Assert.NotEmpty(resources);
        Assert.NotEmpty(costs);
        
        var awsCost = costs.FirstOrDefault(c => c.Provider == "aws");
        Assert.NotNull(awsCost);
        Assert.True(awsCost.TotalMonthlyCost > 0);
        Assert.True(awsCost.TotalAnnualCost > 0);
        Assert.Equal(resources.Count, awsCost.ResourceCount);
        
        // Verify cost breakdown by service
        Assert.NotEmpty(awsCost.CostByService);
        
        // Verify we have costs for different resource types
        var resourceTypes = awsCost.Resources.Select(r => r.ResourceType).Distinct().ToList();
        Assert.True(resourceTypes.Count >= 3); // Should have instance, s3, rds, etc.
    }

    [Fact]
    public async Task ParseAndCalculate_AzureExample_ReturnsValidCosts()
    {
        // Arrange
        var terraformContent = await File.ReadAllTextAsync("TestData/azure-example.tf");

        // Act - Parse
        var resources = await _parser.ParseTerraformAsync(terraformContent);
        
        // Act - Calculate
        var costs = await _calculator.CalculateCostsAsync(resources, "Monthly");

        // Assert
        Assert.NotEmpty(resources);
        Assert.NotEmpty(costs);
        
        var azureCost = costs.FirstOrDefault(c => c.Provider == "azurerm");
        Assert.NotNull(azureCost);
        Assert.True(azureCost.TotalMonthlyCost > 0);
        Assert.True(azureCost.TotalAnnualCost > 0);
        
        // Verify cost breakdown
        Assert.NotEmpty(azureCost.CostByService);
        Assert.NotEmpty(azureCost.Resources);
    }

    [Fact]
    public async Task ParseAndCalculate_GCPExample_ReturnsValidCosts()
    {
        // Arrange
        var terraformContent = await File.ReadAllTextAsync("TestData/gcp-example.tf");

        // Act - Parse
        var resources = await _parser.ParseTerraformAsync(terraformContent);
        
        // Act - Calculate
        var costs = await _calculator.CalculateCostsAsync(resources, "Monthly");

        // Assert
        Assert.NotEmpty(resources);
        Assert.NotEmpty(costs);
        
        var gcpCost = costs.FirstOrDefault(c => c.Provider == "google");
        Assert.NotNull(gcpCost);
        Assert.True(gcpCost.TotalMonthlyCost > 0);
        Assert.True(gcpCost.TotalAnnualCost > 0);
        
        // Verify cost breakdown
        Assert.NotEmpty(gcpCost.CostByService);
        Assert.NotEmpty(gcpCost.Resources);
    }

    [Fact]
    public async Task ParseAndCalculate_MultiCloudExample_ReturnsAllProviders()
    {
        // Arrange
        var awsContent = await File.ReadAllTextAsync("TestData/aws-example.tf");
        var azureContent = await File.ReadAllTextAsync("TestData/azure-example.tf");
        var gcpContent = await File.ReadAllTextAsync("TestData/gcp-example.tf");
        var combinedContent = $"{awsContent}\n\n{azureContent}\n\n{gcpContent}";

        // Act - Parse
        var resources = await _parser.ParseTerraformAsync(combinedContent);
        
        // Act - Calculate
        var costs = await _calculator.CalculateCostsAsync(resources, "Monthly");

        // Assert
        Assert.NotEmpty(resources);
        Assert.Equal(3, costs.Count);
        
        var awsCost = costs.FirstOrDefault(c => c.Provider == "aws");
        var azureCost = costs.FirstOrDefault(c => c.Provider == "azurerm");
        var gcpCost = costs.FirstOrDefault(c => c.Provider == "google");
        
        Assert.NotNull(awsCost);
        Assert.NotNull(azureCost);
        Assert.NotNull(gcpCost);
        
        // All should have costs
        Assert.True(awsCost.TotalMonthlyCost > 0);
        Assert.True(azureCost.TotalMonthlyCost > 0);
        Assert.True(gcpCost.TotalMonthlyCost > 0);
        
        // Verify total across all providers
        var totalMonthly = awsCost.TotalMonthlyCost + azureCost.TotalMonthlyCost + gcpCost.TotalMonthlyCost;
        Assert.True(totalMonthly > 0);
    }

    [Fact]
    public async Task ParseAndCalculate_ResourceAttributes_ArePreserved()
    {
        // Arrange
        var terraformContent = await File.ReadAllTextAsync("TestData/aws-example.tf");

        // Act
        var resources = await _parser.ParseTerraformAsync(terraformContent);
        var costs = await _calculator.CalculateCostsAsync(resources, "Monthly");

        // Assert
        var awsCost = costs.First();
        var instanceResource = awsCost.Resources.FirstOrDefault(r => r.ResourceType == "aws_instance");
        
        Assert.NotNull(instanceResource);
        Assert.NotEmpty(instanceResource.Configuration);
        Assert.Contains(instanceResource.Configuration.Keys, k => k == "instance_type" || k == "region");
    }

    [Fact]
    public async Task ParseAndCalculate_CostByRegion_IsCalculated()
    {
        // Arrange
        var terraformContent = await File.ReadAllTextAsync("TestData/aws-example.tf");

        // Act
        var resources = await _parser.ParseTerraformAsync(terraformContent);
        var costs = await _calculator.CalculateCostsAsync(resources, "Monthly");

        // Assert
        var awsCost = costs.First();
        
        // Should have cost breakdown by region
        Assert.NotEmpty(awsCost.CostByRegion);
        
        // Verify region costs sum to total
        var regionTotal = awsCost.CostByRegion.Values.Sum();
        Assert.True(Math.Abs(regionTotal - awsCost.TotalMonthlyCost) < 0.01m); // Allow for rounding
    }

    [Fact]
    public async Task ParseAndCalculate_CostByService_IsCalculated()
    {
        // Arrange
        var terraformContent = await File.ReadAllTextAsync("TestData/aws-example.tf");

        // Act
        var resources = await _parser.ParseTerraformAsync(terraformContent);
        var costs = await _calculator.CalculateCostsAsync(resources, "Monthly");

        // Assert
        var awsCost = costs.First();
        
        // Should have cost breakdown by service
        Assert.NotEmpty(awsCost.CostByService);
        
        // Verify service costs sum to total
        var serviceTotal = awsCost.CostByService.Values.Sum();
        Assert.True(Math.Abs(serviceTotal - awsCost.TotalMonthlyCost) < 0.01m); // Allow for rounding
        
        // Should have multiple service categories
        Assert.True(awsCost.CostByService.Count >= 2);
    }
}
