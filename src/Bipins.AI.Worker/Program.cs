using Bipins.AI;
using Bipins.AI.Core;
using Bipins.AI.Core.Configuration;
using Bipins.AI.Core.DependencyInjection;
using Bipins.AI.Providers.OpenAI;
using Bipins.AI.Vectors.Qdrant;
using Bipins.AI.Ingestion;
using Bipins.AI.Runtime;
using Bipins.AI.Runtime.Rag;
using Bipins.AI.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        // Add user secrets support in development
        if (context.HostingEnvironment.IsDevelopment())
        {
            config.AddUserSecrets<Program>();
        }
    })
    .ConfigureServices((context, services) =>
    {
        // Add Bipins.AI services
        services.AddBipinsAI();
        services.AddBipinsAIRuntime();
        services.AddBipinsAIIngestion();
        services.AddBipinsAIRag();
        services
            .AddBipinsAI()
            .AddOpenAI(o =>
            {
                o.ApiKey = context.Configuration.GetRequiredValueOrEnvironmentVariable("OpenAI:ApiKey", "OPENAI_API_KEY");
                o.BaseUrl = context.Configuration.GetValueOrEnvironmentVariable("OpenAI:BaseUrl", "OPENAI_BASE_URL") ?? "https://api.openai.com/v1";
                o.DefaultChatModelId = context.Configuration.GetValueOrEnvironmentVariable("OpenAI:DefaultChatModelId", "OPENAI_DEFAULT_CHAT_MODEL_ID") ?? "gpt-3.5-turbo";
                o.DefaultEmbeddingModelId = context.Configuration.GetValueOrEnvironmentVariable("OpenAI:DefaultEmbeddingModelId", "OPENAI_DEFAULT_EMBEDDING_MODEL_ID") ?? "text-embedding-ada-002";
            })
            .AddQdrant(o =>
            {
                o.Endpoint = context.Configuration.GetValueOrEnvironmentVariable("Qdrant:Endpoint", "QDRANT_ENDPOINT") ?? "http://localhost:6333";
                o.ApiKey = context.Configuration.GetValueOrEnvironmentVariable("Qdrant:ApiKey", "QDRANT_API_KEY");
                o.DefaultCollectionName = context.Configuration.GetValueOrEnvironmentVariable("Qdrant:CollectionName", "QDRANT_COLLECTION_NAME") ?? "default";
                o.VectorSize = int.Parse(context.Configuration.GetValueOrEnvironmentVariable("Qdrant:VectorSize", "QDRANT_VECTOR_SIZE") ?? "1536");
                o.CreateCollectionIfMissing = true;
            });

        // Add worker
        services.AddHostedService<IngestionWorker>();
    })
    .Build();

await host.RunAsync();
