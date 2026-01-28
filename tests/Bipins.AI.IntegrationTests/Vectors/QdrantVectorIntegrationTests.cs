using Bipins.AI.Vector;
using Bipins.AI.Vectors.Qdrant;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Bipins.AI.IntegrationTests.Vectors;

[Collection("Integration")]
public class QdrantVectorIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public QdrantVectorIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task QdrantVectorStore_UpsertAndQuery_Works()
    {
        var endpoint = Environment.GetEnvironmentVariable("QDRANT_ENDPOINT");
        if (string.IsNullOrEmpty(endpoint))
        {
            return; // Skip if not configured
        }

        // Create QdrantVectorStore with environment variable endpoint
        var httpClientFactory = _fixture.Services.GetRequiredService<IHttpClientFactory>();
        var logger = _fixture.Services.GetRequiredService<ILogger<QdrantVectorStore>>();
        var options = Options.Create(new QdrantOptions
        {
            Endpoint = endpoint,
            DefaultCollectionName = $"itest_{Guid.NewGuid():N}",
            VectorSize = 3, // Match test vector size
            CreateCollectionIfMissing = true
        });
        
        var store = new QdrantVectorStore(httpClientFactory, options, logger);

        var tenantId = "integration-test-tenant";
        var vector = new ReadOnlyMemory<float>(new float[] { 0.1f, 0.2f, 0.3f });

        var upsertRequest = new VectorUpsertRequest(
            Records: new[]
            {
                new VectorRecord(
                    Id: Guid.NewGuid().ToString("N"),
                    Vector: vector,
                    Text: "integration test vector",
                    Metadata: new Dictionary<string, object>
                    {
                        ["source"] = "integration-test"
                    },
                    TenantId: tenantId,
                    VersionId: "v1")
            },
            CollectionName: options.Value.DefaultCollectionName);

        await store.UpsertAsync(upsertRequest, CancellationToken.None);

        var queryRequest = new VectorQueryRequest(
            QueryVector: vector,
            TopK: 1,
            TenantId: tenantId,
            CollectionName: options.Value.DefaultCollectionName);

        var result = await store.QueryAsync(queryRequest, CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.Matches);
        Assert.True(result.Matches.Count >= 1);
    }
}

