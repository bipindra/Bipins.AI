using System.Text.Json;
using Bipins.AI.Core;
using Bipins.AI.Core.DependencyInjection;
using Bipins.AI.Core.Contracts;
using Bipins.AI.Core.Ingestion;
using Bipins.AI.Core.Models;
using Bipins.AI.Core.Rag;
using Bipins.AI.Connectors.Llm.OpenAI;
using Bipins.AI.Connectors.Vector.Qdrant;
using Bipins.AI.Ingestion;
using Bipins.AI.Runtime;
using Bipins.AI.Runtime.Rag;
using Bipins.AI.Runtime.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
                o.ApiKey = context.Configuration["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new InvalidOperationException("OpenAI:ApiKey not configured");
                o.BaseUrl = context.Configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1";
                o.DefaultChatModelId = context.Configuration["OpenAI:DefaultChatModelId"] ?? "gpt-3.5-turbo";
                o.DefaultEmbeddingModelId = context.Configuration["OpenAI:DefaultEmbeddingModelId"] ?? "text-embedding-ada-002";
            })
            .AddQdrant(o =>
            {
                o.Endpoint = context.Configuration["Qdrant:Endpoint"] ?? Environment.GetEnvironmentVariable("QDRANT_ENDPOINT") ?? "http://localhost:6333";
                o.ApiKey = context.Configuration["Qdrant:ApiKey"];
                o.DefaultCollectionName = context.Configuration["Qdrant:CollectionName"] ?? "default";
                o.VectorSize = int.Parse(context.Configuration["Qdrant:VectorSize"] ?? "1536");
                o.CreateCollectionIfMissing = true;
            });
    })
    .Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
var pipeline = host.Services.GetRequiredService<IngestionPipeline>();
var router = host.Services.GetRequiredService<IModelRouter>();
var retriever = host.Services.GetRequiredService<IRetriever>();
var composer = host.Services.GetRequiredService<IRagComposer>();

logger.LogInformation("Bipins.AI Sample Application");
logger.LogInformation("============================");

// Step 1: Create sample markdown file
var sampleFile = Path.Combine(Path.GetTempPath(), "sample.md");
var sampleContent = @"# Introduction to AI

Artificial Intelligence (AI) is a branch of computer science that aims to create intelligent machines.

## Machine Learning

Machine Learning is a subset of AI that enables systems to learn from data without being explicitly programmed.

### Supervised Learning

Supervised learning uses labeled data to train models. Examples include classification and regression tasks.

### Unsupervised Learning

Unsupervised learning finds patterns in unlabeled data. Clustering is a common example.

## Deep Learning

Deep Learning uses neural networks with multiple layers to learn complex patterns. It has revolutionized fields like computer vision and natural language processing.
";

await File.WriteAllTextAsync(sampleFile, sampleContent);
logger.LogInformation("Created sample markdown file: {File}", sampleFile);

try
{
    // Step 2: Ingest the file
    logger.LogInformation("\nStep 1: Ingesting sample file...");
    var options = new IndexOptions("sample-tenant", "sample-doc-1", null, null);
    var result = await pipeline.IngestAsync(sampleFile, options);
    logger.LogInformation("Ingestion complete: {ChunksIndexed} chunks, {VectorsCreated} vectors", result.ChunksIndexed, result.VectorsCreated);

    // Step 3: Query with RAG
    logger.LogInformation("\nStep 2: Querying with RAG...");
    var query = "What is machine learning?";
    logger.LogInformation("Query: {Query}", query);

    var retrieveRequest = new RetrieveRequest(query, TopK: 3);
    var retrieved = await retriever.RetrieveAsync(retrieveRequest);

    logger.LogInformation("Retrieved {Count} chunks:", retrieved.Chunks.Count);
    foreach (var chunk in retrieved.Chunks)
    {
        logger.LogInformation("  - Score: {Score:F3}, Text: {Text}", chunk.Score, chunk.Chunk.Text.Substring(0, Math.Min(100, chunk.Chunk.Text.Length)) + "...");
    }

    // Step 4: Generate response with RAG
    logger.LogInformation("\nStep 3: Generating response with RAG...");
    var chatRequest = new ChatRequest(new[]
    {
        new Message(MessageRole.User, query)
    });

    var augmentedRequest = composer.Compose(chatRequest, retrieved);
    var chatModel = await router.SelectChatModelAsync("sample-tenant", augmentedRequest);
    var response = await chatModel.GenerateAsync(augmentedRequest);

    logger.LogInformation("\nResponse:");
    logger.LogInformation(response.Content);

    // Step 5: Display citations and telemetry
    logger.LogInformation("\nCitations:");
    var citations = retrieved.Chunks.Select(c => new Citation(
        c.SourceUri,
        c.DocId,
        c.Chunk.Id,
        c.Chunk.Text,
        c.Score)).ToList();

    foreach (var citation in citations)
    {
        logger.LogInformation("  - [{Score:F3}] {Text}", citation.Score, citation.Text.Substring(0, Math.Min(80, citation.Text.Length)) + "...");
    }

    if (response.Usage != null)
    {
        logger.LogInformation("\nTelemetry:");
        logger.LogInformation("  Model: {ModelId}", response.ModelId ?? "unknown");
        logger.LogInformation("  Tokens: {Total} (Prompt: {Prompt}, Completion: {Completion})",
            response.Usage.TotalTokens,
            response.Usage.PromptTokens,
            response.Usage.CompletionTokens);
    }

    logger.LogInformation("\nSample completed successfully!");
}
catch (Exception ex)
{
    logger.LogError(ex, "Error running sample");
    throw;
}
finally
{
    if (File.Exists(sampleFile))
    {
        File.Delete(sampleFile);
    }
}
