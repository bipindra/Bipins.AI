using Bipins.AI.Vector;
using Bipins.AI.Vectors.Pinecone;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bipins.AI.IntegrationTests.Vectors;

[Collection("Integration")]
public class PineconeVectorIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public PineconeVectorIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(Skip = "Requires Pinecone API key and index")]
    public async Task PineconeVectorStore_UpsertAndQuery_Works()
    {
        var apiKey = Environment.GetEnvironmentVariable("PINECONE_API_KEY");
        var indexName = Environment.GetEnvironmentVariable("PINECONE_INDEX");

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(indexName))
        {
            return; // Skip if not configured
        }

        var store = _fixture.Services.GetRequiredService<PineconeVectorStore>();

        var tenantId = "integration-test-tenant";
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
            CollectionName: indexName);

        await store.UpsertAsync(upsertRequest, CancellationToken.None);

        var queryRequest = new VectorQueryRequest(
            QueryVector: vector,
            TopK: 1,
            TenantId: tenantId,
            CollectionName: indexName);

        var result = await store.QueryAsync(queryRequest, CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.Matches);
        Assert.True(result.Matches.Count >= 1);
    }
}

