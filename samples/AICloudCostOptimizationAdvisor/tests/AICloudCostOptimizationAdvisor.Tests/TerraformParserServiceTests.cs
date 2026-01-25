using AICloudCostOptimizationAdvisor.Shared.Models;
using AICloudCostOptimizationAdvisor.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AICloudCostOptimizationAdvisor.Tests;

public class TerraformParserServiceTests
{
    private readonly ITerraformParserService _parser;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Mock<ILogger<TerraformParserService>> _loggerMock;

    public TerraformParserServiceTests()
    {
        _loggerMock = new Mock<ILogger<TerraformParserService>>();
        
        // Use real HttpClientFactory for testing
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddHttpClient();
        var serviceProvider = services.BuildServiceProvider();
        _httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        
        _parser = new TerraformParserService(_httpClientFactory, _loggerMock.Object);
    }

    [Fact]
    public async Task ParseTerraformAsync_AWSResources_ReturnsParsedResources()
    {
        // Arrange
        var terraformContent = await File.ReadAllTextAsync("TestData/aws-example.tf");

        // Act
        var resources = await _parser.ParseTerraformAsync(terraformContent);

        // Assert
        Assert.NotEmpty(resources);
        var awsResources = resources.Where(r => r.CloudProvider == "aws").ToList();
        Assert.NotEmpty(awsResources);
        
        // Check for specific resource types
        Assert.Contains(awsResources, r => r.ResourceType == "aws_instance");
        Assert.Contains(awsResources, r => r.ResourceType == "aws_s3_bucket");
        Assert.Contains(awsResources, r => r.ResourceType == "aws_db_instance");
        Assert.Contains(awsResources, r => r.ResourceType == "aws_lambda_function");
        Assert.Contains(awsResources, r => r.ResourceType == "aws_ebs_volume");
        
        // Verify resource attributes
        var instance = awsResources.FirstOrDefault(r => r.ResourceType == "aws_instance");
        Assert.NotNull(instance);
        Assert.Contains(instance.Attributes.Keys, k => k == "instance_type");
    }

    [Fact]
    public async Task ParseTerraformAsync_AzureResources_ReturnsParsedResources()
    {
        // Arrange
        var terraformContent = await File.ReadAllTextAsync("TestData/azure-example.tf");

        // Act
        var resources = await _parser.ParseTerraformAsync(terraformContent);

        // Assert
        Assert.NotEmpty(resources);
        var azureResources = resources.Where(r => r.CloudProvider == "azurerm").ToList();
        Assert.NotEmpty(azureResources);
        
        // Check for specific resource types
        Assert.Contains(azureResources, r => r.ResourceType == "azurerm_virtual_machine");
        Assert.Contains(azureResources, r => r.ResourceType == "azurerm_storage_account");
        Assert.Contains(azureResources, r => r.ResourceType == "azurerm_sql_database");
        Assert.Contains(azureResources, r => r.ResourceType == "azurerm_function_app");
        
        // Verify resource attributes
        var vm = azureResources.FirstOrDefault(r => r.ResourceType == "azurerm_virtual_machine");
        Assert.NotNull(vm);
        Assert.Contains(vm.Attributes.Keys, k => k == "vm_size");
    }

    [Fact]
    public async Task ParseTerraformAsync_GCPResources_ReturnsParsedResources()
    {
        // Arrange
        var terraformContent = await File.ReadAllTextAsync("TestData/gcp-example.tf");

        // Act
        var resources = await _parser.ParseTerraformAsync(terraformContent);

        // Assert
        Assert.NotEmpty(resources);
        var gcpResources = resources.Where(r => r.CloudProvider == "google").ToList();
        Assert.NotEmpty(gcpResources);
        
        // Check for specific resource types
        Assert.Contains(gcpResources, r => r.ResourceType == "google_compute_instance");
        Assert.Contains(gcpResources, r => r.ResourceType == "google_storage_bucket");
        Assert.Contains(gcpResources, r => r.ResourceType == "google_sql_database_instance");
        Assert.Contains(gcpResources, r => r.ResourceType == "google_cloudfunctions_function");
        
        // Verify resource attributes
        var compute = gcpResources.FirstOrDefault(r => r.ResourceType == "google_compute_instance");
        Assert.NotNull(compute);
        Assert.Contains(compute.Attributes.Keys, k => k == "machine_type");
    }

    [Fact]
    public async Task ParseTerraformAsync_MultiCloud_ReturnsAllProviders()
    {
        // Arrange
        var awsContent = await File.ReadAllTextAsync("TestData/aws-example.tf");
        var azureContent = await File.ReadAllTextAsync("TestData/azure-example.tf");
        var gcpContent = await File.ReadAllTextAsync("TestData/gcp-example.tf");
        var combinedContent = $"{awsContent}\n\n{azureContent}\n\n{gcpContent}";

        // Act
        var resources = await _parser.ParseTerraformAsync(combinedContent);

        // Assert
        Assert.NotEmpty(resources);
        Assert.Contains(resources, r => r.CloudProvider == "aws");
        Assert.Contains(resources, r => r.CloudProvider == "azurerm");
        Assert.Contains(resources, r => r.CloudProvider == "google");
    }

    [Fact]
    public async Task ValidateTerraformAsync_ValidTerraform_ReturnsTrue()
    {
        // Arrange
        var terraformContent = await File.ReadAllTextAsync("TestData/aws-example.tf");

        // Act
        var isValid = await _parser.ValidateTerraformAsync(terraformContent);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public async Task ValidateTerraformAsync_InvalidTerraform_ReturnsFalse()
    {
        // Arrange
        var invalidContent = "This is not valid Terraform code";

        // Act
        var isValid = await _parser.ValidateTerraformAsync(invalidContent);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task ValidateTerraformAsync_EmptyContent_ReturnsFalse()
    {
        // Arrange
        var emptyContent = "";

        // Act
        var isValid = await _parser.ValidateTerraformAsync(emptyContent);

        // Assert
        Assert.False(isValid);
    }
}
