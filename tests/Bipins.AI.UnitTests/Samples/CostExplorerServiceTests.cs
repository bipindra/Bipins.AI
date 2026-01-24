using Amazon.CostExplorer;
using Amazon.CostExplorer.Model;
using AICostOptimizationAdvisor.Shared.Models;
using AICostOptimizationAdvisor.Shared.Services;
using Moq;
using Xunit;

namespace Bipins.AI.UnitTests.Samples;

public class CostExplorerServiceTests
{
    private readonly Mock<IAmazonCostExplorer> _mockCostExplorer;
    private readonly CostExplorerService _service;

    public CostExplorerServiceTests()
    {
        _mockCostExplorer = new Mock<IAmazonCostExplorer>();
        _service = new CostExplorerService(_mockCostExplorer.Object);
    }

    [Fact]
    public async Task GetCostAndUsageAsync_WithValidRequest_ReturnsCostData()
    {
        // Arrange
        var request = new GetCostDataRequest
        {
            StartDate = "2024-01-01",
            EndDate = "2024-01-31",
            Granularity = "DAILY"
        };

        var mockResponse = new GetCostAndUsageResponse
        {
            ResultsByTime = new List<ResultByTime>
            {
                new ResultByTime
                {
                    TimePeriod = new DateInterval { Start = "2024-01-01", End = "2024-01-02" },
                    Groups = new List<Group>
                    {
                        new Group
                        {
                            Keys = new List<string> { "EC2", "us-east-1" },
                            Metrics = new Dictionary<string, MetricValue>
                            {
                                { "BlendedCost", new MetricValue { Amount = "100.50", Unit = "USD" } },
                                { "UsageQuantity", new MetricValue { Amount = "1000", Unit = "Count" } }
                            }
                        }
                    }
                }
            }
        };

        _mockCostExplorer
            .Setup(x => x.GetCostAndUsageAsync(It.IsAny<GetCostAndUsageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _service.GetCostAndUsageAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Costs);
        Assert.Equal(100.50m, result.TotalCost);
        Assert.Equal("USD", result.Currency);
        Assert.Equal("2024-01-01", result.Costs[0].Date);
        Assert.Equal("EC2", result.Costs[0].Service);
        Assert.Equal("us-east-1", result.Costs[0].Region);
        Assert.Equal(100.50m, result.Costs[0].Amount);
    }

    [Fact]
    public async Task GetCostAndUsageAsync_WithServiceFilter_AppliesFilter()
    {
        // Arrange
        var request = new GetCostDataRequest
        {
            StartDate = "2024-01-01",
            EndDate = "2024-01-31",
            Granularity = "DAILY",
            Services = new List<string> { "EC2", "S3" }
        };

        var mockResponse = new GetCostAndUsageResponse
        {
            ResultsByTime = new List<ResultByTime>()
        };

        _mockCostExplorer
            .Setup(x => x.GetCostAndUsageAsync(It.Is<GetCostAndUsageRequest>(r => r.Filter != null), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        // Act
        await _service.GetCostAndUsageAsync(request);

        // Assert
        _mockCostExplorer.Verify(x => x.GetCostAndUsageAsync(
            It.Is<GetCostAndUsageRequest>(r => 
                r.Filter != null && 
                r.Filter.Dimensions != null &&
                r.Filter.Dimensions.Values != null &&
                r.Filter.Dimensions.Values.Contains("EC2") &&
                r.Filter.Dimensions.Values.Contains("S3")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCostAndUsageAsync_WithMultipleGroups_CalculatesTotalCost()
    {
        // Arrange
        var request = new GetCostDataRequest
        {
            StartDate = "2024-01-01",
            EndDate = "2024-01-31",
            Granularity = "DAILY"
        };

        var mockResponse = new GetCostAndUsageResponse
        {
            ResultsByTime = new List<ResultByTime>
            {
                new ResultByTime
                {
                    TimePeriod = new DateInterval { Start = "2024-01-01", End = "2024-01-02" },
                    Groups = new List<Group>
                    {
                        new Group
                        {
                            Keys = new List<string> { "EC2", "us-east-1" },
                            Metrics = new Dictionary<string, MetricValue>
                            {
                                { "BlendedCost", new MetricValue { Amount = "100.00", Unit = "USD" } }
                            }
                        },
                        new Group
                        {
                            Keys = new List<string> { "S3", "us-west-2" },
                            Metrics = new Dictionary<string, MetricValue>
                            {
                                { "BlendedCost", new MetricValue { Amount = "50.00", Unit = "USD" } }
                            }
                        }
                    }
                }
            }
        };

        _mockCostExplorer
            .Setup(x => x.GetCostAndUsageAsync(It.IsAny<GetCostAndUsageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _service.GetCostAndUsageAsync(request);

        // Assert
        Assert.Equal(150.00m, result.TotalCost);
        Assert.Equal(2, result.Costs.Count);
    }

    [Fact]
    public async Task GetCostAndUsageAsync_WithMissingRegion_DefaultsToUnknown()
    {
        // Arrange
        var request = new GetCostDataRequest
        {
            StartDate = "2024-01-01",
            EndDate = "2024-01-31",
            Granularity = "DAILY"
        };

        var mockResponse = new GetCostAndUsageResponse
        {
            ResultsByTime = new List<ResultByTime>
            {
                new ResultByTime
                {
                    TimePeriod = new DateInterval { Start = "2024-01-01", End = "2024-01-02" },
                    Groups = new List<Group>
                    {
                        new Group
                        {
                            Keys = new List<string> { "EC2" }, // Only service, no region
                            Metrics = new Dictionary<string, MetricValue>
                            {
                                { "BlendedCost", new MetricValue { Amount = "100.00", Unit = "USD" } }
                            }
                        }
                    }
                }
            }
        };

        _mockCostExplorer
            .Setup(x => x.GetCostAndUsageAsync(It.IsAny<GetCostAndUsageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _service.GetCostAndUsageAsync(request);

        // Assert
        Assert.Equal("Unknown", result.Costs[0].Region);
    }

    [Fact]
    public async Task GetCostAndUsageAsync_WithEmptyResponse_ReturnsEmptyCostData()
    {
        // Arrange
        var request = new GetCostDataRequest
        {
            StartDate = "2024-01-01",
            EndDate = "2024-01-31",
            Granularity = "DAILY"
        };

        var mockResponse = new GetCostAndUsageResponse
        {
            ResultsByTime = new List<ResultByTime>()
        };

        _mockCostExplorer
            .Setup(x => x.GetCostAndUsageAsync(It.IsAny<GetCostAndUsageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _service.GetCostAndUsageAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Costs);
        Assert.Equal(0m, result.TotalCost);
        Assert.Equal("USD", result.Currency);
    }
}
