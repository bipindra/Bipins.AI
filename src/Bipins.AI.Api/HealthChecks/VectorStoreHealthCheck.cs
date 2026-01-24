using Bipins.AI.Core.Vector;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Bipins.AI.Api.HealthChecks;

/// <summary>
/// Health check for vector store connectivity.
/// </summary>
public class VectorStoreHealthCheck : IHealthCheck
{
    private readonly IVectorStore _vectorStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="VectorStoreHealthCheck"/> class.
    /// </summary>
    public VectorStoreHealthCheck(IVectorStore vectorStore)
    {
        _vectorStore = vectorStore;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to query with a dummy vector to check connectivity
            var dummyVector = new float[1536].AsMemory();
            var queryRequest = new VectorQueryRequest(dummyVector, TopK: 1);
            await _vectorStore.QueryAsync(queryRequest, cancellationToken);
            
            return HealthCheckResult.Healthy("Vector store is accessible");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Vector store is not accessible", ex);
        }
    }
}
