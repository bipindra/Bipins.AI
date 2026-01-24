using System.Diagnostics;
using System.Diagnostics.Metrics;
using Bipins.AI.Core.Models;

namespace Bipins.AI.Runtime.Observability;

/// <summary>
/// Metrics collector for Bipins.AI operations.
/// </summary>
public class MetricsCollector
{
    private readonly Meter _meter;
    private readonly Counter<long> _requestCount;
    private readonly Histogram<double> _requestLatency;
    private readonly Counter<long> _errorCount;
    private readonly Counter<long> _tokenUsage;
    private readonly Counter<long> _embeddingCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetricsCollector"/> class.
    /// </summary>
    public MetricsCollector()
    {
        _meter = new Meter("Bipins.AI", "1.0.0");
        _requestCount = _meter.CreateCounter<long>("bipins_requests_total", "count", "Total number of requests");
        _requestLatency = _meter.CreateHistogram<double>("bipins_request_latency_seconds", "seconds", "Request latency in seconds");
        _errorCount = _meter.CreateCounter<long>("bipins_errors_total", "count", "Total number of errors");
        _tokenUsage = _meter.CreateCounter<long>("bipins_tokens_total", "tokens", "Total token usage");
        _embeddingCount = _meter.CreateCounter<long>("bipins_embeddings_total", "count", "Total number of embeddings generated");
    }

    /// <summary>
    /// Records a request.
    /// </summary>
    public void RecordRequest(string operation, string? modelId = null, string? tenantId = null)
    {
        var tags = new TagList
        {
            { "operation", operation }
        };

        if (!string.IsNullOrEmpty(modelId))
        {
            tags.Add("model", modelId);
        }

        if (!string.IsNullOrEmpty(tenantId))
        {
            tags.Add("tenant", tenantId);
        }

        _requestCount.Add(1, tags);
    }

    /// <summary>
    /// Records request latency.
    /// </summary>
    public void RecordLatency(string operation, double seconds, string? modelId = null, string? tenantId = null)
    {
        var tags = new TagList
        {
            { "operation", operation }
        };

        if (!string.IsNullOrEmpty(modelId))
        {
            tags.Add("model", modelId);
        }

        if (!string.IsNullOrEmpty(tenantId))
        {
            tags.Add("tenant", tenantId);
        }

        _requestLatency.Record(seconds, tags);
    }

    /// <summary>
    /// Records an error.
    /// </summary>
    public void RecordError(string operation, string errorType, string? modelId = null, string? tenantId = null)
    {
        var tags = new TagList
        {
            { "operation", operation },
            { "error_type", errorType }
        };

        if (!string.IsNullOrEmpty(modelId))
        {
            tags.Add("model", modelId);
        }

        if (!string.IsNullOrEmpty(tenantId))
        {
            tags.Add("tenant", tenantId);
        }

        _errorCount.Add(1, tags);
    }

    /// <summary>
    /// Records token usage.
    /// </summary>
    public void RecordTokenUsage(Usage usage, string? modelId = null, string? tenantId = null)
    {
        if (usage == null) return;

        var tags = new TagList
        {
            { "type", "total" }
        };

        if (!string.IsNullOrEmpty(modelId))
        {
            tags.Add("model", modelId);
        }

        if (!string.IsNullOrEmpty(tenantId))
        {
            tags.Add("tenant", tenantId);
        }

        _tokenUsage.Add(usage.TotalTokens, tags);
    }

    /// <summary>
    /// Records embedding generation.
    /// </summary>
    public void RecordEmbedding(int count, string? modelId = null, string? tenantId = null)
    {
        var tags = new TagList();

        if (!string.IsNullOrEmpty(modelId))
        {
            tags.Add("model", modelId);
        }

        if (!string.IsNullOrEmpty(tenantId))
        {
            tags.Add("tenant", tenantId);
        }

        _embeddingCount.Add(count, tags);
    }
}
