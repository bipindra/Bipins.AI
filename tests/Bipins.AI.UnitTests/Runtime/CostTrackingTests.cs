using Bipins.AI.Core.CostTracking;
using Bipins.AI.Runtime.CostTracking;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bipins.AI.UnitTests.Runtime;

public class CostTrackingTests
{
    private readonly Mock<ILogger<DefaultCostCalculator>> _calculatorLogger;
    private readonly Mock<ILogger<InMemoryCostTracker>> _trackerLogger;
    private readonly DefaultCostCalculator _calculator;
    private readonly InMemoryCostTracker _tracker;

    public CostTrackingTests()
    {
        _calculatorLogger = new Mock<ILogger<DefaultCostCalculator>>();
        _trackerLogger = new Mock<ILogger<InMemoryCostTracker>>();
        _calculator = new DefaultCostCalculator(_calculatorLogger.Object);
        _tracker = new InMemoryCostTracker(_trackerLogger.Object);
    }

    [Fact]
    public void DefaultCostCalculator_CalculateChatCost_OpenAI_ReturnsCorrectCost()
    {
        var cost = _calculator.CalculateChatCost("OpenAI", "gpt-4", 1000, 500);

        // (1000/1000) * 0.03 + (500/1000) * 0.06 = 0.03 + 0.03 = 0.06
        Assert.Equal(0.06m, cost);
    }

    [Fact]
    public void DefaultCostCalculator_CalculateChatCost_Anthropic_ReturnsCorrectCost()
    {
        var cost = _calculator.CalculateChatCost("Anthropic", "claude-3-sonnet", 2000, 1000);

        // (2000/1000) * 0.003 + (1000/1000) * 0.015 = 0.006 + 0.015 = 0.021
        Assert.Equal(0.021m, cost);
    }

    [Fact]
    public void DefaultCostCalculator_CalculateChatCost_Azure_ReturnsCorrectCost()
    {
        var cost = _calculator.CalculateChatCost("Azure", "gpt-4", 1000, 500);

        Assert.Equal(0.06m, cost);
    }

    [Fact]
    public void DefaultCostCalculator_CalculateChatCost_Bedrock_ReturnsCorrectCost()
    {
        var cost = _calculator.CalculateChatCost("Bedrock", "anthropic.claude-3-haiku", 1000, 500);

        // (1000/1000) * 0.00025 + (500/1000) * 0.00125 = 0.00025 + 0.000625 = 0.000875
        Assert.Equal(0.000875m, cost);
    }

    [Fact]
    public void DefaultCostCalculator_CalculateChatCost_UnknownProvider_ReturnsZero()
    {
        var cost = _calculator.CalculateChatCost("UnknownProvider", "model1", 1000, 500);

        Assert.Equal(0m, cost);
    }

    [Fact]
    public void DefaultCostCalculator_CalculateChatCost_UnknownModel_ReturnsZero()
    {
        var cost = _calculator.CalculateChatCost("OpenAI", "unknown-model", 1000, 500);

        Assert.Equal(0m, cost);
    }

    [Fact]
    public void DefaultCostCalculator_CalculateChatCost_PartialModelMatch_ReturnsCost()
    {
        // "gpt-4-32k" should match "gpt-4"
        var cost = _calculator.CalculateChatCost("OpenAI", "gpt-4-32k", 1000, 500);

        Assert.Equal(0.06m, cost);
    }

    [Fact]
    public void DefaultCostCalculator_CalculateEmbeddingCost_OpenAI_ReturnsCorrectCost()
    {
        var cost = _calculator.CalculateEmbeddingCost("OpenAI", "text-embedding-ada-002", 1000);

        // (1000/1000) * 0.0001 = 0.0001
        Assert.Equal(0.0001m, cost);
    }

    [Fact]
    public void DefaultCostCalculator_CalculateEmbeddingCost_UnknownProvider_ReturnsZero()
    {
        var cost = _calculator.CalculateEmbeddingCost("UnknownProvider", "model1", 1000);

        Assert.Equal(0m, cost);
    }

    [Fact]
    public void DefaultCostCalculator_CalculateStorageCost_ReturnsCorrectCost()
    {
        // 1 GB = 1,000,000,000 bytes
        var cost = _calculator.CalculateStorageCost(1_000_000_000);

        // (1 GB * $0.10) / 30 days = $0.00333...
        Assert.True(cost > 0m && cost < 0.01m);
    }

    [Fact]
    public void DefaultCostCalculator_CalculateQueryCost_Qdrant_ReturnsZero()
    {
        var cost = _calculator.CalculateQueryCost("Qdrant", 100);

        Assert.Equal(0m, cost);
    }

    [Fact]
    public void DefaultCostCalculator_CalculateQueryCost_UnknownProvider_ReturnsZero()
    {
        var cost = _calculator.CalculateQueryCost("UnknownProvider", 100);

        Assert.Equal(0m, cost);
    }

    [Fact]
    public async Task InMemoryCostTracker_RecordCostAsync_AddsRecord()
    {
        var record = new CostRecord("id1", "tenant1", CostOperationType.Chat, "OpenAI", "gpt-4", 100, 50, 50, null, 1, 0.01m);

        await _tracker.RecordCostAsync(record);

        var records = await _tracker.GetCostRecordsAsync("tenant1", DateTimeOffset.MinValue, DateTimeOffset.MaxValue);
        Assert.Single(records);
        Assert.Equal(record, records[0]);
    }

    [Fact]
    public async Task InMemoryCostTracker_GetCostRecordsAsync_FiltersByTenant()
    {
        var record1 = new CostRecord("id1", "tenant1", CostOperationType.Chat, "OpenAI");
        var record2 = new CostRecord("id2", "tenant2", CostOperationType.Chat, "OpenAI");

        await _tracker.RecordCostAsync(record1);
        await _tracker.RecordCostAsync(record2);

        var records = await _tracker.GetCostRecordsAsync("tenant1", DateTimeOffset.MinValue, DateTimeOffset.MaxValue);
        Assert.Single(records);
        Assert.Equal(record1, records[0]);
    }

    [Fact]
    public async Task InMemoryCostTracker_GetCostRecordsAsync_FiltersByTimeRange()
    {
        var startTime = DateTimeOffset.UtcNow.AddDays(-2);
        var middleTime = DateTimeOffset.UtcNow.AddDays(-1);
        var endTime = DateTimeOffset.UtcNow;

        var record1 = new CostRecord("id1", "tenant1", CostOperationType.Chat, "OpenAI", null, null, null, null, null, 1, 0m, startTime.AddHours(-1));
        var record2 = new CostRecord("id2", "tenant1", CostOperationType.Chat, "OpenAI", null, null, null, null, null, 1, 0m, middleTime);
        var record3 = new CostRecord("id3", "tenant1", CostOperationType.Chat, "OpenAI", null, null, null, null, null, 1, 0m, endTime.AddHours(1));

        await _tracker.RecordCostAsync(record1);
        await _tracker.RecordCostAsync(record2);
        await _tracker.RecordCostAsync(record3);

        var records = await _tracker.GetCostRecordsAsync("tenant1", startTime, endTime);
        Assert.Single(records);
        Assert.Equal(record2, records[0]);
    }

    [Fact]
    public async Task InMemoryCostTracker_GetCostSummaryAsync_CalculatesTotals()
    {
        var startTime = DateTimeOffset.UtcNow.AddDays(-1);
        var endTime = DateTimeOffset.UtcNow;

        await _tracker.RecordCostAsync(new CostRecord("id1", "tenant1", CostOperationType.Chat, "OpenAI", "gpt-4", 100, 50, 50, null, 1, 0.01m, startTime));
        await _tracker.RecordCostAsync(new CostRecord("id2", "tenant1", CostOperationType.Embedding, "OpenAI", "text-embedding-ada-002", 200, null, null, null, 1, 0.02m, startTime));

        var summary = await _tracker.GetCostSummaryAsync("tenant1", startTime, endTime);

        Assert.Equal("tenant1", summary.TenantId);
        Assert.Equal(0.03m, summary.TotalCost);
        Assert.Equal(300, summary.TotalTokens);
        Assert.Equal(2, summary.TotalApiCalls);
    }

    [Fact]
    public async Task InMemoryCostTracker_GetCostSummaryAsync_BreaksDownByOperation()
    {
        var startTime = DateTimeOffset.UtcNow.AddDays(-1);
        var endTime = DateTimeOffset.UtcNow;

        await _tracker.RecordCostAsync(new CostRecord("id1", "tenant1", CostOperationType.Chat, "OpenAI", null, null, null, null, null, 1, 0.5m, startTime));
        await _tracker.RecordCostAsync(new CostRecord("id2", "tenant1", CostOperationType.Embedding, "OpenAI", null, null, null, null, null, 1, 0.2m, startTime));

        var summary = await _tracker.GetCostSummaryAsync("tenant1", startTime, endTime);

        Assert.Equal(0.5m, summary.CostByOperation[CostOperationType.Chat]);
        Assert.Equal(0.2m, summary.CostByOperation[CostOperationType.Embedding]);
    }

    [Fact]
    public async Task InMemoryCostTracker_GetCostSummaryAsync_BreaksDownByProvider()
    {
        var startTime = DateTimeOffset.UtcNow.AddDays(-1);
        var endTime = DateTimeOffset.UtcNow;

        await _tracker.RecordCostAsync(new CostRecord("id1", "tenant1", CostOperationType.Chat, "OpenAI", null, null, null, null, null, 1, 0.6m, startTime));
        await _tracker.RecordCostAsync(new CostRecord("id2", "tenant1", CostOperationType.Chat, "Anthropic", null, null, null, null, null, 1, 0.1m, startTime));

        var summary = await _tracker.GetCostSummaryAsync("tenant1", startTime, endTime);

        Assert.Equal(0.6m, summary.CostByProvider["OpenAI"]);
        Assert.Equal(0.1m, summary.CostByProvider["Anthropic"]);
    }

    [Fact]
    public async Task InMemoryCostTracker_GetCostSummaryAsync_BreaksDownByModel()
    {
        var startTime = DateTimeOffset.UtcNow.AddDays(-1);
        var endTime = DateTimeOffset.UtcNow;

        await _tracker.RecordCostAsync(new CostRecord("id1", "tenant1", CostOperationType.Chat, "OpenAI", "gpt-4", null, null, null, null, 1, 0.5m, startTime));
        await _tracker.RecordCostAsync(new CostRecord("id2", "tenant1", CostOperationType.Chat, "OpenAI", "gpt-3.5-turbo", null, null, null, null, 1, 0.1m, startTime));

        var summary = await _tracker.GetCostSummaryAsync("tenant1", startTime, endTime);

        Assert.Equal(0.5m, summary.CostByModel["gpt-4"]);
        Assert.Equal(0.1m, summary.CostByModel["gpt-3.5-turbo"]);
    }

    [Fact]
    public async Task InMemoryCostTracker_GetCostSummaryAsync_EmptyResults_ReturnsZeroSummary()
    {
        var startTime = DateTimeOffset.UtcNow.AddDays(-1);
        var endTime = DateTimeOffset.UtcNow;

        var summary = await _tracker.GetCostSummaryAsync("tenant1", startTime, endTime);

        Assert.Equal("tenant1", summary.TenantId);
        Assert.Equal(0m, summary.TotalCost);
        Assert.Equal(0, summary.TotalTokens);
        Assert.Equal(0, summary.TotalApiCalls);
        Assert.Empty(summary.CostByOperation);
        Assert.Empty(summary.CostByProvider);
        Assert.Empty(summary.CostByModel);
    }
}
