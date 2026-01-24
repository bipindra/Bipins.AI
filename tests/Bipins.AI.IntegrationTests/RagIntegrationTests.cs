using Bipins.AI.Core.Ingestion;
using Bipins.AI.Core.Models;
using Bipins.AI.Core.Rag;
using Bipins.AI.Ingestion;
using Bipins.AI.Runtime.Rag;
using Bipins.AI.Runtime.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bipins.AI.IntegrationTests;

[Collection("Integration")]
public class RagIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public RagIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(Skip = "Requires OpenAI API key")]
    public async Task EndToEndRagFlow_RetrievesAndComposes()
    {
        var pipeline = _fixture.Services.GetRequiredService<IngestionPipeline>();
        var retriever = _fixture.Services.GetRequiredService<IRetriever>();
        var composer = _fixture.Services.GetRequiredService<IRagComposer>();
        var router = _fixture.Services.GetRequiredService<IModelRouter>();

        // Create test file
        var testFile = Path.Combine(Path.GetTempPath(), $"rag_test_{Guid.NewGuid()}.md");
        var content = @"# AI Fundamentals

Artificial intelligence is the simulation of human intelligence by machines.

## Neural Networks

Neural networks are computing systems inspired by biological neural networks.
";
        await File.WriteAllTextAsync(testFile, content);

        try
        {
            // Ingest
            var options = new IndexOptions("test-tenant", "rag-doc", null, null);
            await pipeline.IngestAsync(testFile, options);

            await Task.Delay(1000);

            // Retrieve
            var retrieveRequest = new RetrieveRequest("neural networks", "test-tenant", TopK: 2);
            var retrieved = await retriever.RetrieveAsync(retrieveRequest);

            Assert.True(retrieved.Chunks.Count > 0);

            // Compose
            var originalRequest = new ChatRequest(new[]
            {
                new Message(MessageRole.User, "What are neural networks?")
            });

            var augmentedRequest = composer.Compose(originalRequest, retrieved);

            Assert.True(augmentedRequest.Messages.Count > originalRequest.Messages.Count);
            Assert.Contains(augmentedRequest.Messages, m => m.Role == MessageRole.System);
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
