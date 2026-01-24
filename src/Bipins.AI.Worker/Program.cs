using Bipins.AI.Core;
using Bipins.AI.Core.DependencyInjection;
using Bipins.AI.Connectors.Llm.OpenAI;
using Bipins.AI.Connectors.Vector.Qdrant;
using Bipins.AI.Ingestion;
using Bipins.AI.Runtime;
using Bipins.AI.Runtime.Rag;
using Bipins.AI.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
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
                o.ApiKey = context.Configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI:ApiKey not configured");
                o.BaseUrl = context.Configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1";
            })
            .AddQdrant(o =>
            {
                o.Endpoint = context.Configuration["Qdrant:Endpoint"] ?? "http://localhost:6333";
                o.DefaultCollectionName = context.Configuration["Qdrant:CollectionName"] ?? "default";
                o.VectorSize = int.Parse(context.Configuration["Qdrant:VectorSize"] ?? "1536");
            });

        // Add worker
        services.AddHostedService<IngestionWorker>();
    })
    .Build();

await host.RunAsync();
