using Bipins.AI.Core.Models;
using Bipins.AI.Runtime.Observability;
using Xunit;

namespace Bipins.AI.UnitTests.Runtime;

public class ObservabilityTests
{
    [Fact]
    public void MetricsCollector_RecordRequest_DoesNotThrow()
    {
        var collector = new MetricsCollector();

        collector.RecordRequest("chat");
        collector.RecordRequest("embedding", "text-embedding-ada-002", "tenant1");
    }

    [Fact]
    public void MetricsCollector_RecordLatency_DoesNotThrow()
    {
        var collector = new MetricsCollector();

        collector.RecordLatency("chat", 1.5);
        collector.RecordLatency("embedding", 0.5, "text-embedding-ada-002", "tenant1");
    }

    [Fact]
    public void MetricsCollector_RecordError_DoesNotThrow()
    {
        var collector = new MetricsCollector();

        collector.RecordError("chat", "timeout");
        collector.RecordError("embedding", "invalid_request", "text-embedding-ada-002", "tenant1");
    }

    [Fact]
    public void MetricsCollector_RecordTokenUsage_DoesNotThrow()
    {
        var collector = new MetricsCollector();
        var usage = new Usage(100, 50, 150);

        collector.RecordTokenUsage(usage);
        collector.RecordTokenUsage(usage, "gpt-4", "tenant1");
    }

    [Fact]
    public void MetricsCollector_RecordTokenUsage_WithNullUsage_DoesNotThrow()
    {
        var collector = new MetricsCollector();

        collector.RecordTokenUsage(null);
    }

    [Fact]
    public void MetricsCollector_RecordEmbedding_DoesNotThrow()
    {
        var collector = new MetricsCollector();

        collector.RecordEmbedding(1);
        collector.RecordEmbedding(10, "text-embedding-ada-002", "tenant1");
    }

    [Fact]
    public void MetricsCollector_MultipleOperations_DoesNotThrow()
    {
        var collector = new MetricsCollector();
        var usage = new Usage(100, 50, 150);

        collector.RecordRequest("chat", "gpt-4", "tenant1");
        collector.RecordLatency("chat", 1.5, "gpt-4", "tenant1");
        collector.RecordTokenUsage(usage, "gpt-4", "tenant1");
        collector.RecordEmbedding(5, "text-embedding-ada-002", "tenant1");
        collector.RecordError("chat", "rate_limit", "gpt-4", "tenant1");
    }
}
