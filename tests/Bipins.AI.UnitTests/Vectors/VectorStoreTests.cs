using Bipins.AI.Vectors.Qdrant;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Bipins.AI.UnitTests.Vectors;

public class VectorStoreTests
{
    [Fact]
    public void QdrantVectorStore_CanBeInstantiated()
    {
        var httpClientFactory = new Mock<IHttpClientFactory>();
        var logger = new Mock<ILogger<QdrantVectorStore>>();
        var options = Options.Create(new QdrantOptions
        {
            Endpoint = "http://localhost:6333",
            DefaultCollectionName = "test"
        });

        var vectorStore = new QdrantVectorStore(httpClientFactory.Object, options, logger.Object);

        Assert.NotNull(vectorStore);
    }
}
