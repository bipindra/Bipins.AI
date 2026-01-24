using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using AICostOptimizationAdvisor.Shared.Models;
using AICostOptimizationAdvisor.Shared.Services;
using Moq;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Bipins.AI.UnitTests.Samples;

public class BedrockAnalysisServiceEdgeCaseTests
{
    private readonly Mock<IAmazonBedrockRuntime> _mockBedrock;
    private readonly BedrockAnalysisService _service;

    public BedrockAnalysisServiceEdgeCaseTests()
    {
        _mockBedrock = new Mock<IAmazonBedrockRuntime>();
        _service = new BedrockAnalysisService(_mockBedrock.Object, "anthropic.claude-3-sonnet-20240229-v1:0");
    }

    [Fact]
    public async Task AnalyzeCostsAsync_WithEmptyCostData_HandlesGracefully()
    {
        // Arrange
        var costData = new GetCostDataResponse
        {
            Costs = new List<CostData>(),
            TotalCost = 0m,
            DateRange = "2024-01-01 to 2024-01-31"
        };

        var analysisJson = @"{""costDrivers"":[],""anomalies"":[],""suggestions"":[],""summary"":""No costs found""}";
        var bedrockResponseJson = new { content = new[] { new { text = analysisJson } } };

        var bedrockResponse = new InvokeModelResponse
        {
            Body = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(bedrockResponseJson)))
        };

        _mockBedrock
            .Setup(x => x.InvokeModelAsync(It.IsAny<InvokeModelRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(bedrockResponse);

        // Act
        var result = await _service.AnalyzeCostsAsync(costData);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0m, result.TotalCost);
        Assert.Empty(result.CostDrivers);
    }

    [Fact]
    public async Task AnalyzeCostsAsync_WithNullModelId_UsesDefault()
    {
        // Arrange
        var costData = new GetCostDataResponse
        {
            Costs = new List<CostData>(),
            TotalCost = 0m,
            DateRange = "2024-01-01 to 2024-01-31"
        };

        var analysisJson = @"{""costDrivers"":[],""anomalies"":[],""suggestions"":[],""summary"":""Test""}";
        var bedrockResponseJson = new { content = new[] { new { text = analysisJson } } };

        var bedrockResponse = new InvokeModelResponse
        {
            Body = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(bedrockResponseJson)))
        };

        _mockBedrock
            .Setup(x => x.InvokeModelAsync(It.Is<InvokeModelRequest>(r => r.ModelId == "anthropic.claude-3-sonnet-20240229-v1:0"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(bedrockResponse);

        // Act
        await _service.AnalyzeCostsAsync(costData, null);

        // Assert
        _mockBedrock.Verify(x => x.InvokeModelAsync(
            It.Is<InvokeModelRequest>(r => r.ModelId == "anthropic.claude-3-sonnet-20240229-v1:0"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AnalyzeCostsAsync_WithPartialJson_ExtractsCorrectly()
    {
        // Arrange
        var costData = new GetCostDataResponse
        {
            Costs = new List<CostData>(),
            TotalCost = 0m,
            DateRange = "2024-01-01 to 2024-01-31"
        };

        var partialJson = @"{""costDrivers"":[],""anomalies"":[]"; // Incomplete JSON
        var bedrockResponseJson = new { content = new[] { new { text = partialJson } } };

        var bedrockResponse = new InvokeModelResponse
        {
            Body = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(bedrockResponseJson)))
        };

        _mockBedrock
            .Setup(x => x.InvokeModelAsync(It.IsAny<InvokeModelRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(bedrockResponse);

        // Act
        var result = await _service.AnalyzeCostsAsync(costData);

        // Assert
        Assert.NotNull(result);
        // Should fall back to basic analysis when JSON parsing fails
        Assert.Contains("unexpected", result.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AnalyzeCostsAsync_WithLargeCostData_BuildsPromptCorrectly()
    {
        // Arrange
        var costData = new GetCostDataResponse
        {
            Costs = Enumerable.Range(1, 100).Select(i => new CostData
            {
                Service = $"Service{i}",
                Amount = i * 10m,
                Date = $"2024-01-{i:D2}"
            }).ToList(),
            TotalCost = 50500m,
            DateRange = "2024-01-01 to 2024-01-31"
        };

        var analysisJson = @"{""costDrivers"":[],""anomalies"":[],""suggestions"":[],""summary"":""Large dataset analyzed""}";
        var bedrockResponseJson = new { content = new[] { new { text = analysisJson } } };

        var bedrockResponse = new InvokeModelResponse
        {
            Body = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(bedrockResponseJson)))
        };

        _mockBedrock
            .Setup(x => x.InvokeModelAsync(It.IsAny<InvokeModelRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(bedrockResponse);

        // Act
        var result = await _service.AnalyzeCostsAsync(costData);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(50500m, result.TotalCost);
        _mockBedrock.Verify(x => x.InvokeModelAsync(
            It.Is<InvokeModelRequest>(r => r.Body != null && r.Body.Length > 0),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AnalyzeCostsAsync_WithNullContent_ThrowsException()
    {
        // Arrange
        var costData = new GetCostDataResponse
        {
            Costs = new List<CostData>(),
            TotalCost = 0m,
            DateRange = "2024-01-01 to 2024-01-31"
        };

        var bedrockResponse = new InvokeModelResponse
        {
            Body = new MemoryStream(Encoding.UTF8.GetBytes(@"{""content"":null}"))
        };

        _mockBedrock
            .Setup(x => x.InvokeModelAsync(It.IsAny<InvokeModelRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(bedrockResponse);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.AnalyzeCostsAsync(costData));
    }

    [Fact]
    public async Task AnalyzeCostsAsync_WithEmptyText_HandlesGracefully()
    {
        // Arrange
        var costData = new GetCostDataResponse
        {
            Costs = new List<CostData>(),
            TotalCost = 0m,
            DateRange = "2024-01-01 to 2024-01-31"
        };

        var bedrockResponseJson = new { content = new[] { new { text = "" } } };

        var bedrockResponse = new InvokeModelResponse
        {
            Body = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(bedrockResponseJson)))
        };

        _mockBedrock
            .Setup(x => x.InvokeModelAsync(It.IsAny<InvokeModelRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(bedrockResponse);

        // Act
        var result = await _service.AnalyzeCostsAsync(costData);

        // Assert
        Assert.NotNull(result);
        // Should create fallback analysis when text is empty
        Assert.Contains("unexpected", result.Summary, StringComparison.OrdinalIgnoreCase);
    }
}
