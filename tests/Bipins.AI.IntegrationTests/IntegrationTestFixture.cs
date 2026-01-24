using Bipins.AI.Core;
using Bipins.AI.Connectors.Llm.OpenAI;
using Bipins.AI.Connectors.Vector.Qdrant;
using Bipins.AI.Ingestion;
using Bipins.AI.Runtime;
using Bipins.AI.Runtime.Rag;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Bipins.AI.IntegrationTests;

[CollectionDefinition("Integration")]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
}

public class IntegrationTestFixture : IDisposable
{
    public IServiceProvider Services { get; }

    public IntegrationTestFixture()
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Use test configuration
                services.AddBipinsAI();
                services.AddBipinsAIRuntime();
                services.AddBipinsAIIngestion();
                services.AddBipinsAIRag();
                services
                    .AddBipinsAI()
                    .AddOpenAI(o =>
                    {
                        // Use environment variable or test key
                        o.ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "test-key";
                        o.BaseUrl = "https://api.openai.com/v1";
                        o.DefaultChatModelId = "gpt-3.5-turbo";
                        o.DefaultEmbeddingModelId = "text-embedding-ada-002";
                    })
                    .AddQdrant(o =>
                    {
                        o.Endpoint = Environment.GetEnvironmentVariable("QDRANT_ENDPOINT") ?? "http://localhost:6333";
                        o.DefaultCollectionName = $"test_{Guid.NewGuid()}";
                        o.VectorSize = 1536;
                        o.CreateCollectionIfMissing = true;
                    });
            })
            .Build();

        Services = host.Services;
    }

    public void Dispose()
    {
        if (Services is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
