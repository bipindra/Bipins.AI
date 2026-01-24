using AICostOptimizationAdvisor.Shared.Models;
using AICostOptimizationAdvisor.Shared.Services;
using Bipins.AI.Core.Models;
using Moq;
using System.Text.Json;
using Xunit;

namespace Bipins.AI.UnitTests.Samples;

public class BedrockAnalysisServiceTests
{
    private readonly Mock<IChatModel> _mockChatModel;
    private readonly BedrockAnalysisService _service;

    public BedrockAnalysisServiceTests()
    {
        _mockChatModel = new Mock<IChatModel>();
        _service = new BedrockAnalysisService(_mockChatModel.Object, "anthropic.claude-3-sonnet-20240229-v1:0");
    }

    [Fact]
    public async Task AnalyzeCostsAsync_WithValidResponse_ReturnsCostAnalysis()
    {
        // Arrange
        var costData = new GetCostDataResponse
        {
            Costs = new List<CostData>
            {
                new CostData { Service = "EC2", Amount = 100.00m, Date = "2024-01-01" }
            },
            TotalCost = 100.00m,
            DateRange = "2024-01-01 to 2024-01-31"
        };

        var analysisJson = @"{
            ""costDrivers"": [
                {
                    ""service"": ""EC2"",
                    ""region"": ""us-east-1"",
                    ""amount"": 100.00,
                    ""percentage"": 100.0,
                    ""description"": ""EC2 is the main cost driver""
                }
            ],
            ""anomalies"": [],
            ""suggestions"": [
                {
                    ""category"": ""Compute"",
                    ""description"": ""Consider using Reserved Instances"",
                    ""estimatedSavings"": ""$20-30/month"",
                    ""priority"": ""High"",
                    ""actions"": [""action1""],
                    ""service"": ""EC2""
                }
            ],
            ""summary"": ""EC2 is the primary cost driver""
        }";

        var chatResponse = new ChatResponse(analysisJson);

        _mockChatModel
            .Setup(x => x.GenerateAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatResponse);

        // Act
        var result = await _service.AnalyzeCostsAsync(costData);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.AnalysisId);
        Assert.Equal(costData.DateRange, result.DateRange);
        Assert.Equal(costData.TotalCost, result.TotalCost);
        Assert.Single(result.CostDrivers);
        Assert.Single(result.Suggestions);
    }

    [Fact]
    public async Task AnalyzeCostsAsync_WithCustomModelId_UsesCustomModel()
    {
        // Arrange
        var costData = new GetCostDataResponse
        {
            Costs = new List<CostData>(),
            TotalCost = 0m,
            DateRange = "2024-01-01 to 2024-01-31"
        };

        var customModelId = "anthropic.claude-3-haiku-20240307-v1:0";
        var chatResponse = new ChatResponse(@"{}");

        _mockChatModel
            .Setup(x => x.GenerateAsync(It.Is<ChatRequest>(r => r.Metadata != null && r.Metadata.ContainsKey("modelId") && r.Metadata["modelId"].ToString() == customModelId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatResponse);

        // Act
        await _service.AnalyzeCostsAsync(costData, customModelId);

        // Assert
        _mockChatModel.Verify(x => x.GenerateAsync(
            It.Is<ChatRequest>(r => r.Metadata != null && r.Metadata.ContainsKey("modelId") && r.Metadata["modelId"].ToString() == customModelId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AnalyzeCostsAsync_WithInvalidResponse_ThrowsException()
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
    public async Task AnalyzeCostsAsync_WithJsonInMarkdown_ExtractsJson()
    {
        // Arrange
        var costData = new GetCostDataResponse
        {
            Costs = new List<CostData>(),
            TotalCost = 0m,
            DateRange = "2024-01-01 to 2024-01-31"
        };

        var analysisJson = @"{""costDrivers"":[],""anomalies"":[],""suggestions"":[],""summary"":""Test""}";
        var markdownResponse = $@"Here is the analysis:

```json
{analysisJson}
```

This is the end.";

        var chatResponse = new ChatResponse(markdownResponse);

        _mockChatModel
            .Setup(x => x.GenerateAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatResponse);

        // Act
        var result = await _service.AnalyzeCostsAsync(costData);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test", result.Summary);
    }

    [Fact]
    public async Task AnalyzeCostsAsync_WithJsonParseError_CreatesFallbackAnalysis()
    {
        // Arrange
        var costData = new GetCostDataResponse
        {
            Costs = new List<CostData>(),
            TotalCost = 100.00m,
            DateRange = "2024-01-01 to 2024-01-31"
        };

        var invalidJson = "This is not valid JSON {";
        var chatResponse = new ChatResponse(invalidJson);

        _mockChatModel
            .Setup(x => x.GenerateAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatResponse);

        // Act
        var result = await _service.AnalyzeCostsAsync(costData);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(100.00m, result.TotalCost);
        Assert.Empty(result.CostDrivers);
        Assert.Empty(result.Anomalies);
        Assert.Empty(result.Suggestions);
        Assert.Contains("unexpected", result.Summary, StringComparison.OrdinalIgnoreCase);
    }
}
