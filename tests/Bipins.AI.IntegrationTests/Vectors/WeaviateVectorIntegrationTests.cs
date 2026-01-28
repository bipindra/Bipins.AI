using Bipins.AI.Vector;
using Bipins.AI.Vectors.Weaviate;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bipins.AI.IntegrationTests.Vectors;

[Collection("Integration")]
public class WeaviateVectorIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public WeaviateVectorIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(Skip = "Requires Weaviate endpoint and API key")]
    public async Task WeaviateVectorStore_UpsertAndQuery_Works()
    {
        var endpoint = Environment.GetEnvironmentVariable("WEAVIATE_ENDPOINT");
        var apiKey = Environment.GetEnvironmentVariable("WEAVIATE_API_KEY");

        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
        {
            return; // Skip if not configured
        }

        var store = _fixture.Services.GetRequiredService<WeaviateVectorStore>();

        var tenantId = "integration-test-tenant";
        var className = Environment.GetEnvironmentVariable("WEAVIATE_CLASS") ?? "IntegrationTestClass";
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
            CollectionName: className);

        await store.UpsertAsync(upsertRequest, CancellationToken.None);

        var queryRequest = new VectorQueryRequest(
            QueryVector: vector,
            TopK: 1,
            TenantId: tenantId,
            CollectionName: className);

        var result = await store.QueryAsync(queryRequest, CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.Matches);
        Assert.True(result.Matches.Count >= 1);
    }
}

