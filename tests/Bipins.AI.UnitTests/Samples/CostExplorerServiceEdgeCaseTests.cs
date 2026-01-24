using Amazon.CostExplorer;
using Amazon.CostExplorer.Model;
using AICostOptimizationAdvisor.Shared.Models;
using AICostOptimizationAdvisor.Shared.Services;
using Moq;
using Xunit;

namespace Bipins.AI.UnitTests.Samples;

public class CostExplorerServiceEdgeCaseTests
{
    private readonly Mock<IAmazonCostExplorer> _mockCostExplorer;
    private readonly CostExplorerService _service;

    public CostExplorerServiceEdgeCaseTests()
    {
        _mockCostExplorer = new Mock<IAmazonCostExplorer>();
        _service = new CostExplorerService(_mockCostExplorer.Object);
    }

    [Fact]
    public async Task GetCostAndUsageAsync_WithNullMetrics_HandlesGracefully()
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
                                { "BlendedCost", new MetricValue { Amount = "100.50", Unit = "USD" } }
                                // Missing UsageQuantity metric
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
        Assert.Equal("0", result.Costs[0].UsageQuantity); // Should default to "0"
    }

    [Fact]
    public async Task GetCostAndUsageAsync_WithEmptyGroups_ReturnsEmptyCostData()
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
                    Groups = new List<Group>()
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
        Assert.Empty(result.Costs);
        Assert.Equal(0m, result.TotalCost);
    }

    [Fact]
    public async Task GetCostAndUsageAsync_WithNullCurrency_DefaultsToUSD()
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
                                { "BlendedCost", new MetricValue { Amount = "100.50", Unit = null } }
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
        Assert.Equal("USD", result.Currency);
        Assert.Equal("USD", result.Costs[0].Currency);
    }

    [Fact]
    public async Task GetCostAndUsageAsync_WithMultipleTimePeriods_AggregatesCorrectly()
    {
        // Arrange
        var request = new GetCostDataRequest
        {
            StartDate = "2024-01-01",
            EndDate = "2024-01-03",
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
                        }
                    }
                },
                new ResultByTime
                {
                    TimePeriod = new DateInterval { Start = "2024-01-02", End = "2024-01-03" },
                    Groups = new List<Group>
                    {
                        new Group
                        {
                            Keys = new List<string> { "EC2", "us-east-1" },
                            Metrics = new Dictionary<string, MetricValue>
                            {
                                { "BlendedCost", new MetricValue { Amount = "150.00", Unit = "USD" } }
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
        Assert.Equal(250.00m, result.TotalCost);
        Assert.Equal(2, result.Costs.Count);
    }

    [Fact]
    public async Task GetCostAndUsageAsync_WithNullKeys_HandlesGracefully()
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
                            Keys = new List<string> { null, null }, // Null keys
                            Metrics = new Dictionary<string, MetricValue>
                            {
                                { "BlendedCost", new MetricValue { Amount = "100.50", Unit = "USD" } }
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
        Assert.Equal("Unknown", result.Costs[0].Service);
        Assert.Equal("Unknown", result.Costs[0].Region);
    }
}
