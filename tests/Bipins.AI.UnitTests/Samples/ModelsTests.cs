using AICostOptimizationAdvisor.Shared.Models;
using Xunit;

namespace Bipins.AI.UnitTests.Samples;

public class ModelsTests
{
    [Fact]
    public void CostData_Properties_SetCorrectly()
    {
        // Arrange & Act
        var costData = new CostData
        {
            Date = "2024-01-01",
            Service = "EC2",
            Region = "us-east-1",
            Amount = 100.50m,
            Currency = "USD",
            UsageType = "DataTransfer",
            UsageQuantity = "1000"
        };

        // Assert
        Assert.Equal("2024-01-01", costData.Date);
        Assert.Equal("EC2", costData.Service);
        Assert.Equal("us-east-1", costData.Region);
        Assert.Equal(100.50m, costData.Amount);
        Assert.Equal("USD", costData.Currency);
        Assert.Equal("DataTransfer", costData.UsageType);
        Assert.Equal("1000", costData.UsageQuantity);
    }

    [Fact]
    public void GetCostDataRequest_DefaultValues_AreSet()
    {
        // Arrange & Act
        var request = new GetCostDataRequest();

        // Assert
        Assert.Equal("DAILY", request.Granularity);
        Assert.Empty(request.StartDate);
        Assert.Empty(request.EndDate);
    }

    [Fact]
    public void GetCostDataResponse_DefaultValues_AreSet()
    {
        // Arrange & Act
        var response = new GetCostDataResponse();

        // Assert
        Assert.NotNull(response.Costs);
        Assert.Empty(response.Costs);
        Assert.Equal(0m, response.TotalCost);
        Assert.Equal("USD", response.Currency);
        Assert.Empty(response.DateRange);
    }

    [Fact]
    public void CostDriver_Properties_SetCorrectly()
    {
        // Arrange & Act
        var driver = new CostDriver
        {
            Service = "EC2",
            Region = "us-east-1",
            Amount = 100.00m,
            Percentage = 50.5m,
            Description = "EC2 is a major cost driver"
        };

        // Assert
        Assert.Equal("EC2", driver.Service);
        Assert.Equal("us-east-1", driver.Region);
        Assert.Equal(100.00m, driver.Amount);
        Assert.Equal(50.5m, driver.Percentage);
        Assert.Equal("EC2 is a major cost driver", driver.Description);
    }

    [Fact]
    public void CostAnomaly_Properties_SetCorrectly()
    {
        // Arrange & Act
        var anomaly = new CostAnomaly
        {
            Date = "2024-01-15",
            Service = "EC2",
            ExpectedAmount = 100.00m,
            ActualAmount = 250.00m,
            Variance = 150.00m,
            Description = "Unexpected spike in EC2 costs"
        };

        // Assert
        Assert.Equal("2024-01-15", anomaly.Date);
        Assert.Equal("EC2", anomaly.Service);
        Assert.Equal(100.00m, anomaly.ExpectedAmount);
        Assert.Equal(250.00m, anomaly.ActualAmount);
        Assert.Equal(150.00m, anomaly.Variance);
        Assert.Equal("Unexpected spike in EC2 costs", anomaly.Description);
    }

    [Fact]
    public void OptimizationSuggestion_Properties_SetCorrectly()
    {
        // Arrange & Act
        var suggestion = new OptimizationSuggestion
        {
            Category = "Compute",
            Description = "Use Reserved Instances",
            EstimatedSavings = "$50-100/month",
            Priority = "High",
            Actions = new List<string> { "action1", "action2" },
            Service = "EC2"
        };

        // Assert
        Assert.Equal("Compute", suggestion.Category);
        Assert.Equal("Use Reserved Instances", suggestion.Description);
        Assert.Equal("$50-100/month", suggestion.EstimatedSavings);
        Assert.Equal("High", suggestion.Priority);
        Assert.NotNull(suggestion.Actions);
        Assert.Equal(2, suggestion.Actions.Count);
        Assert.Equal("EC2", suggestion.Service);
    }

    [Fact]
    public void OptimizationSuggestion_DefaultPriority_IsMedium()
    {
        // Arrange & Act
        var suggestion = new OptimizationSuggestion();

        // Assert
        Assert.Equal("Medium", suggestion.Priority);
    }

    [Fact]
    public void CostAnalysis_Properties_SetCorrectly()
    {
        // Arrange & Act
        var analysis = new CostAnalysis
        {
            AnalysisId = "analysis-123",
            DateRange = "2024-01-01 to 2024-01-31",
            CreatedAt = DateTime.UtcNow,
            CostDrivers = new List<CostDriver> { new CostDriver() },
            Anomalies = new List<CostAnomaly> { new CostAnomaly() },
            Suggestions = new List<OptimizationSuggestion> { new OptimizationSuggestion() },
            TotalCost = 1000.00m,
            Summary = "Test summary"
        };

        // Assert
        Assert.Equal("analysis-123", analysis.AnalysisId);
        Assert.Equal("2024-01-01 to 2024-01-31", analysis.DateRange);
        Assert.Single(analysis.CostDrivers);
        Assert.Single(analysis.Anomalies);
        Assert.Single(analysis.Suggestions);
        Assert.Equal(1000.00m, analysis.TotalCost);
        Assert.Equal("Test summary", analysis.Summary);
    }

    [Fact]
    public void AnalyzeCostsRequest_Properties_SetCorrectly()
    {
        // Arrange & Act
        var request = new AnalyzeCostsRequest
        {
            CostData = new GetCostDataResponse { TotalCost = 100.00m },
            ModelId = "anthropic.claude-3-sonnet-20240229-v1:0"
        };

        // Assert
        Assert.NotNull(request.CostData);
        Assert.Equal(100.00m, request.CostData.TotalCost);
        Assert.Equal("anthropic.claude-3-sonnet-20240229-v1:0", request.ModelId);
    }

    [Fact]
    public void AnalyzeCostsResponse_Properties_SetCorrectly()
    {
        // Arrange & Act
        var response = new AnalyzeCostsResponse
        {
            Analysis = new CostAnalysis { AnalysisId = "test-123" },
            Success = true,
            ErrorMessage = null
        };

        // Assert
        Assert.NotNull(response.Analysis);
        Assert.Equal("test-123", response.Analysis.AnalysisId);
        Assert.True(response.Success);
        Assert.Null(response.ErrorMessage);
    }

    [Fact]
    public void AnalyzeCostsResponse_WithError_SetsErrorMessage()
    {
        // Arrange & Act
        var response = new AnalyzeCostsResponse
        {
            Analysis = new CostAnalysis(),
            Success = false,
            ErrorMessage = "Test error"
        };

        // Assert
        Assert.False(response.Success);
        Assert.Equal("Test error", response.ErrorMessage);
    }
}
