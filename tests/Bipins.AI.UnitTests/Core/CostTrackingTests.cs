using Bipins.AI.Core.CostTracking;
using Xunit;

namespace Bipins.AI.UnitTests.Core;

public class CostTrackingTests
{
    [Fact]
    public void CostRecord_Creation_SetsProperties()
    {
        var metadata = new Dictionary<string, object> { { "key", "value" } };
        var timestamp = DateTimeOffset.UtcNow;
        var record = new CostRecord(
            Id: "id1",
            TenantId: "tenant1",
            OperationType: CostOperationType.Chat,
            Provider: "OpenAI",
            ModelId: "gpt-4",
            TokensUsed: 100,
            PromptTokens: 50,
            CompletionTokens: 50,
            StorageBytes: 1000,
            ApiCalls: 2,
            Cost: 0.01m,
            Timestamp: timestamp,
            Metadata: metadata);

        Assert.Equal("id1", record.Id);
        Assert.Equal("tenant1", record.TenantId);
        Assert.Equal(CostOperationType.Chat, record.OperationType);
        Assert.Equal("OpenAI", record.Provider);
        Assert.Equal("gpt-4", record.ModelId);
        Assert.Equal(100, record.TokensUsed);
        Assert.Equal(50, record.PromptTokens);
        Assert.Equal(50, record.CompletionTokens);
        Assert.Equal(1000, record.StorageBytes);
        Assert.Equal(2, record.ApiCalls);
        Assert.Equal(0.01m, record.Cost);
        Assert.Equal(timestamp, record.Timestamp);
        Assert.Equal(metadata, record.Metadata);
    }

    [Fact]
    public void CostRecord_Minimal_CreatesRecord()
    {
        var record = new CostRecord("id1", "tenant1", CostOperationType.Chat, "OpenAI");

        Assert.Equal("id1", record.Id);
        Assert.Equal("tenant1", record.TenantId);
        Assert.Equal(CostOperationType.Chat, record.OperationType);
        Assert.Equal("OpenAI", record.Provider);
        Assert.Null(record.ModelId);
        Assert.Null(record.TokensUsed);
        Assert.Equal(1, record.ApiCalls);
        Assert.Equal(0m, record.Cost);
        Assert.NotEqual(default(DateTimeOffset), record.Timestamp);
    }

    [Fact]
    public void CostRecord_Timestamp_DefaultsToUtcNow()
    {
        var before = DateTimeOffset.UtcNow;
        var record = new CostRecord("id1", "tenant1", CostOperationType.Chat, "OpenAI");
        var after = DateTimeOffset.UtcNow;

        Assert.True(record.Timestamp >= before && record.Timestamp <= after);
    }

    [Fact]
    public void CostRecord_AllOperationTypes_AreValid()
    {
        var types = Enum.GetValues<CostOperationType>();
        
        foreach (var type in types)
        {
            var record = new CostRecord("id1", "tenant1", type, "Provider");
            Assert.Equal(type, record.OperationType);
        }
    }

    [Fact]
    public void CostSummary_Creation_SetsProperties()
    {
        var startTime = DateTimeOffset.UtcNow.AddDays(-1);
        var endTime = DateTimeOffset.UtcNow;
        var costByOperation = new Dictionary<CostOperationType, decimal>
        {
            { CostOperationType.Chat, 0.5m },
            { CostOperationType.Embedding, 0.2m }
        };
        var costByProvider = new Dictionary<string, decimal>
        {
            { "OpenAI", 0.6m },
            { "Anthropic", 0.1m }
        };
        var costByModel = new Dictionary<string, decimal>
        {
            { "gpt-4", 0.5m },
            { "gpt-3.5-turbo", 0.1m }
        };

        var summary = new CostSummary(
            TenantId: "tenant1",
            StartTime: startTime,
            EndTime: endTime,
            TotalCost: 0.7m,
            TotalTokens: 1000,
            TotalApiCalls: 10,
            TotalStorageBytes: 5000,
            CostByOperation: costByOperation,
            CostByProvider: costByProvider,
            CostByModel: costByModel);

        Assert.Equal("tenant1", summary.TenantId);
        Assert.Equal(startTime, summary.StartTime);
        Assert.Equal(endTime, summary.EndTime);
        Assert.Equal(0.7m, summary.TotalCost);
        Assert.Equal(1000, summary.TotalTokens);
        Assert.Equal(10, summary.TotalApiCalls);
        Assert.Equal(5000, summary.TotalStorageBytes);
        Assert.Equal(costByOperation, summary.CostByOperation);
        Assert.Equal(costByProvider, summary.CostByProvider);
        Assert.Equal(costByModel, summary.CostByModel);
    }

    [Fact]
    public void CostSummary_EmptyBreakdowns_CreatesSummary()
    {
        var startTime = DateTimeOffset.UtcNow.AddDays(-1);
        var endTime = DateTimeOffset.UtcNow;

        var summary = new CostSummary(
            "tenant1",
            startTime,
            endTime,
            0m,
            0,
            0,
            0,
            new Dictionary<CostOperationType, decimal>(),
            new Dictionary<string, decimal>(),
            new Dictionary<string, decimal>());

        Assert.Equal(0m, summary.TotalCost);
        Assert.Empty(summary.CostByOperation);
        Assert.Empty(summary.CostByProvider);
        Assert.Empty(summary.CostByModel);
    }
}
