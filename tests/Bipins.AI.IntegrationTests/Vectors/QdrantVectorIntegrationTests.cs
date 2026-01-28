using Bipins.AI.Vector;
using Bipins.AI.Vectors.Qdrant;
using Microsoft.Extensions.DependencyInjection;
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

    [Fact(Skip = "Requires Qdrant endpoint and running instance")]
    public async Task QdrantVectorStore_UpsertAndQuery_Works()
    {
        var endpoint = Environment.GetEnvironmentVariable("QDRANT_ENDPOINT");
        if (string.IsNullOrEmpty(endpoint))
        {
            return; // Skip if not configured
        }

        var store = _fixture.Services.GetRequiredService<QdrantVectorStore>();

        var tenantId = "integration-test-tenant";
        var collectionName = $"itest_{Guid.NewGuid():N}";
        var vector = new ReadOnlyMemory<float>(new float[] { 0.1f, 0.2f, 0.3f });

        var upsertRequest = new VectorUpsertRequest(
            Records: new[]
            {
                new VectorRecord(
                    Id: Guid.NewGuid().ToString("N"),
                    Vector: vector,
                    Text: "integration test vector",
                    Metadata: new Dictionary<string, object?>
                    {
                        ["source"] = "integration-test"
                    },
                    TenantId: tenantId,
                    VersionId: "v1")
            },
            CollectionName: collectionName);

        await store.UpsertAsync(upsertRequest, CancellationToken.None);

        var queryRequest = new VectorQueryRequest(
            QueryVector: vector,
            TopK: 1,
            TenantId: tenantId,
            CollectionName: collectionName);

        var result = await store.QueryAsync(queryRequest, CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.Matches);
        Assert.True(result.Matches.Count >= 1);
    }
}

