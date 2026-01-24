using Bipins.AI.Core.Ingestion;
using Bipins.AI.Core.Rag;
using Bipins.AI.Core.Vector;
using Bipins.AI.Ingestion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Bipins.AI.IntegrationTests;

[Collection("Integration")]
public class IngestionIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public IngestionIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Ingest_ThenQuery_ReturnsExpectedCitations()
    {
        var pipeline = _fixture.Services.GetRequiredService<IngestionPipeline>();
        var retriever = _fixture.Services.GetRequiredService<IRetriever>();

        // Create test file
        var testFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.md");
        var content = @"# Test Document

This is a test document about artificial intelligence.

## Machine Learning

Machine learning is a subset of AI that enables systems to learn from data.
";
        await File.WriteAllTextAsync(testFile, content);

        try
        {
            // Ingest
            var options = new IndexOptions("test-tenant", "test-doc", null, null);
            var result = await pipeline.IngestAsync(testFile, options);

            Assert.True(result.ChunksIndexed > 0);
            Assert.True(result.VectorsCreated > 0);

            // Wait a bit for indexing to complete
            await Task.Delay(1000);

            // Query
            var retrieveRequest = new RetrieveRequest("machine learning", "test-tenant", TopK: 3);
            var retrieved = await retriever.RetrieveAsync(retrieveRequest);

            Assert.True(retrieved.Chunks.Count > 0);
            Assert.True(retrieved.Chunks[0].Score > 0);
        }
        finally
        {
            if (File.Exists(testFile))
            {
                File.Delete(testFile);
            }
        }
    }
}
