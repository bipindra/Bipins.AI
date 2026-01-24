using AICostOptimizationAdvisor.Shared.Models;
using AICostOptimizationAdvisor.Shared.Services;
using Bipins.AI.Core.Models;
using Moq;
using System.Text.Json;
using Xunit;

namespace Bipins.AI.UnitTests.Samples;

public class BedrockAnalysisServiceEdgeCaseTests
{
    private readonly Mock<IChatModel> _mockChatModel;
    private readonly BedrockAnalysisService _service;

    public BedrockAnalysisServiceEdgeCaseTests()
    {
        _mockChatModel = new Mock<IChatModel>();
        _service = new BedrockAnalysisService(_mockChatModel.Object, "anthropic.claude-3-sonnet-20240229-v1:0");
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
        var chatResponse = new ChatResponse(analysisJson);

        _mockChatModel
            .Setup(x => x.GenerateAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatResponse);

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
        var chatResponse = new ChatResponse(analysisJson);

        _mockChatModel
            .Setup(x => x.GenerateAsync(It.Is<ChatRequest>(r => r.Metadata != null && r.Metadata.ContainsKey("modelId") && r.Metadata["modelId"].ToString() == "anthropic.claude-3-sonnet-20240229-v1:0"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatResponse);

        // Act
        await _service.AnalyzeCostsAsync(costData, null);

        // Assert
        _mockChatModel.Verify(x => x.GenerateAsync(
            It.Is<ChatRequest>(r => r.Metadata != null && r.Metadata.ContainsKey("modelId") && r.Metadata["modelId"].ToString() == "anthropic.claude-3-sonnet-20240229-v1:0"),
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
        var chatResponse = new ChatResponse(partialJson);

        _mockChatModel
            .Setup(x => x.GenerateAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatResponse);

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
        var chatResponse = new ChatResponse(analysisJson);

        _mockChatModel
            .Setup(x => x.GenerateAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatResponse);

        // Act
        var result = await _service.AnalyzeCostsAsync(costData);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(50500m, result.TotalCost);
        _mockChatModel.Verify(x => x.GenerateAsync(
            It.IsAny<ChatRequest>(),
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

        var chatResponse = new ChatResponse("");

        _mockChatModel
            .Setup(x => x.GenerateAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatResponse);

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

        var chatResponse = new ChatResponse("");

        _mockChatModel
            .Setup(x => x.GenerateAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatResponse);

        // Act
        var result = await _service.AnalyzeCostsAsync(costData);

        // Assert
        Assert.NotNull(result);
        // Should create fallback analysis when text is empty
        Assert.Contains("unexpected", result.Summary, StringComparison.OrdinalIgnoreCase);
    }
}
