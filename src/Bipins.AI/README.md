# Bipins.AI

Enterprise AI platform for building intelligent applications with RAG (Retrieval-Augmented Generation), multiple LLM providers, and vector databases.

[![NuGet](https://img.shields.io/nuget/v/Bipins.AI.svg)](https://www.nuget.org/packages/Bipins.AI)
[![.NET](https://img.shields.io/badge/.NET-Standard%202.1%20%7C%207.0%20%7C%208.0%20%7C%209.0%20%7C%2010.0-purple.svg)](https://dotnet.microsoft.com/download)

## Installation

```bash
dotnet add package Bipins.AI
```

## Quick Start

### Basic Setup

```csharp
using Bipins.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices((context, services) =>
{
    services
        .AddBipinsAI()
        .AddOpenAI(o =>
        {
            o.ApiKey = context.Configuration["OpenAI:ApiKey"] 
                      ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            o.DefaultChatModelId = "gpt-4";
            o.DefaultEmbeddingModelId = "text-embedding-3-small";
        })
        .AddBipinsAIRuntime(context.Configuration)
        .AddBipinsAIIngestion()
        .AddBipinsAIRag();
});

var host = builder.Build();
```

### Chat Completion

```csharp
using Bipins.AI.Core.Models;

var chatModel = serviceProvider.GetRequiredService<IChatModel>();

var request = new ChatRequest(
    Messages: new[]
    {
        new Message(MessageRole.System, "You are a helpful assistant."),
        new Message(MessageRole.User, "What is machine learning?")
    },
    Temperature: 0.7f,
    MaxTokens: 1000);

var response = await chatModel.GenerateAsync(request);
Console.WriteLine(response.Content);
```

### Streaming

```csharp
var streamingModel = serviceProvider.GetRequiredService<IChatModelStreaming>();

await foreach (var chunk in streamingModel.GenerateStreamAsync(request))
{
    Console.Write(chunk.Content);
}
```

### Function Calling / Tools

```csharp
var request = new ChatRequest(
    Messages: new[] { new Message(MessageRole.User, "What's the weather in Seattle?") },
    Tools: new[]
    {
        new ToolDefinition(
            "get_weather",
            "Get weather for a location",
            JsonSerializer.SerializeToElement(new
            {
                type = "object",
                properties = new
                {
                    location = new { type = "string", description = "City name" }
                },
                required = new[] { "location" }
            }))
    });

var response = await chatModel.GenerateAsync(request);

if (response.ToolCalls?.Count > 0)
{
    foreach (var toolCall in response.ToolCalls)
    {
        Console.WriteLine($"Tool: {toolCall.Name}, Args: {toolCall.Arguments}");
    }
}
```

### Structured Output

```csharp
var schema = JsonSerializer.SerializeToElement(new
{
    type = "object",
    properties = new
    {
        name = new { type = "string" },
        age = new { type = "integer" }
    }
});

var request = new ChatRequest(
    Messages: new[] { new Message(MessageRole.User, "Extract person info") },
    StructuredOutput: new StructuredOutputOptions(schema, "json_schema"));

var response = await chatModel.GenerateAsync(request);
var personData = response.StructuredOutput; // JsonElement
```

### Embeddings

```csharp
var embeddingModel = serviceProvider.GetRequiredService<IEmbeddingModel>();

var request = new EmbeddingRequest(
    Inputs: new[] { "Machine learning is..." },
    ModelId: "text-embedding-3-small");

var response = await embeddingModel.EmbedAsync(request);
var vector = response.Vectors[0].ToArray(); // float[]
```

### Vector Store

```csharp
using Bipins.AI.Vector;

// Register vector store
services
    .AddBipinsAI()
    .AddQdrant(o =>
    {
        o.Endpoint = "http://localhost:6333";
        o.CollectionName = "documents";
        o.VectorSize = 1536;
    });

// Upsert vectors
var vectorStore = serviceProvider.GetRequiredService<IVectorStore>();

await vectorStore.UpsertAsync(new VectorUpsertRequest(
    Records: new[]
    {
        new VectorRecord(
            Id: "doc1",
            Vector: new ReadOnlyMemory<float>(embeddingVector),
            Text: "Document content...",
            Metadata: new Dictionary<string, object> { { "source", "file.pdf" } },
            TenantId: "tenant1")
    },
    CollectionName: "documents"));

// Query vectors
var queryResult = await vectorStore.QueryAsync(new VectorQueryRequest(
    QueryVector: new ReadOnlyMemory<float>(queryVector),
    TopK: 5,
    TenantId: "tenant1",
    Filter: VectorFilterBuilder
        .Create()
        .Equal("category", "technical")
        .GreaterThan("date", "2024-01-01")
        .Build()));

foreach (var match in queryResult.Matches)
{
    Console.WriteLine($"Score: {match.Score}, Text: {match.Record.Text}");
}
```

### RAG (Retrieval-Augmented Generation)

```csharp
using Bipins.AI.Core.Rag;

var retriever = serviceProvider.GetRequiredService<IRetriever>();
var ragComposer = serviceProvider.GetRequiredService<IRagComposer>();

// Retrieve relevant chunks
var retrieveResult = await retriever.RetrieveAsync(new RetrieveRequest(
    Query: "What is machine learning?",
    TenantId: "tenant1",
    TopK: 5));

// Compose RAG response
var ragResult = await ragComposer.ComposeAsync(
    query: "What is machine learning?",
    retrievedChunks: retrieveResult.Chunks,
    chatModel: chatModel);

Console.WriteLine(ragResult.Content);
foreach (var citation in ragResult.Citations)
{
    Console.WriteLine($"Source: {citation.SourceUri}, Score: {citation.Score}");
}
```

### Document Ingestion

```csharp
using Bipins.AI.Core.Ingestion;

var ingestionPipeline = serviceProvider.GetRequiredService<IngestionPipeline>();

var document = new Document(
    SourceUri: "https://example.com/doc.pdf",
    Content: File.ReadAllBytes("doc.pdf"),
    MimeType: "application/pdf",
    Metadata: new Dictionary<string, object> { { "title", "AI Guide" } });

var result = await ingestionPipeline.ProcessAsync(
    document: document,
    options: new IndexOptions(
        TenantId: "tenant1",
        DocId: "doc1",
        UpdateMode: UpdateMode.Upsert));

Console.WriteLine($"Chunks indexed: {result.ChunksIndexed}");
```

### ILLMProvider Interface

```csharp
using Bipins.AI.Providers;

var llmProvider = serviceProvider.GetRequiredService<ILLMProvider>();

// Simple chat
var response = await llmProvider.ChatAsync(new ChatRequest(
    Messages: new[] { new Message(MessageRole.User, "Hello") }));

// Streaming
await foreach (var chunk in llmProvider.ChatStreamAsync(new ChatRequest(
    Messages: new[] { new Message(MessageRole.User, "Hello") })))
{
    Console.Write(chunk.Content);
}

// Embeddings
var embedding = await llmProvider.GenerateEmbeddingAsync("text to embed");
```

### ChatService (High-Level API)

```csharp
using Bipins.AI.LLM;

var chatService = new ChatService(
    llmProvider: llmProvider,
    options: new ChatServiceOptions
    {
        Model = "gpt-4",
        Temperature = 0.7,
        MaxTokens = 2000
    },
    logger: loggerFactory.CreateLogger<ChatService>());

// Simple chat
var response = await chatService.ChatAsync(
    systemPrompt: "You are a helpful assistant.",
    userMessage: "What is AI?");

// With tools
var responseWithTools = await chatService.ChatWithToolsAsync(
    systemPrompt: "You are a helpful assistant.",
    userMessage: "Get weather for Seattle",
    tools: new[]
    {
        new ToolDefinition("get_weather", "Get weather", /* schema */)
    });

// Streaming
await foreach (var chunk in chatService.ChatStreamAsync(
    systemPrompt: "You are a helpful assistant.",
    userMessage: "Tell me a story"))
{
    Console.Write(chunk.Content);
}
```

## Supported Providers

### LLM Providers

- **OpenAI**: GPT-4, GPT-3.5, Embeddings
- **Anthropic**: Claude 3 (Opus, Sonnet, Haiku)
- **Azure OpenAI**: GPT-4, GPT-3.5, Embeddings
- **AWS Bedrock**: Claude, Llama, Titan models

### Vector Databases

- **Qdrant**: Open-source vector database
- **Pinecone**: Managed vector database
- **Weaviate**: Open-source vector search engine
- **Milvus**: Open-source vector database

## Configuration

### Provider Options

```csharp
// OpenAI
services.AddBipinsAI().AddOpenAI(o =>
{
    o.ApiKey = "sk-...";
    o.BaseUrl = "https://api.openai.com/v1";
    o.DefaultChatModelId = "gpt-4";
    o.DefaultEmbeddingModelId = "text-embedding-3-small";
});

// Anthropic
services.AddBipinsAI().AddAnthropic(o =>
{
    o.ApiKey = "sk-ant-...";
    o.DefaultChatModelId = "claude-3-5-sonnet-20241022";
});

// Azure OpenAI
services.AddBipinsAI().AddAzureOpenAI(o =>
{
    o.Endpoint = "https://your-resource.openai.azure.com";
    o.ApiKey = "your-key";
    o.DefaultChatDeploymentName = "gpt-4";
    o.DefaultEmbeddingDeploymentName = "text-embedding-ada-002";
});

// AWS Bedrock
services.AddBipinsAI().AddBedrock(o =>
{
    o.Region = "us-east-1";
    o.DefaultModelId = "anthropic.claude-3-5-sonnet-20241022-v2:0";
    // Optional: o.AccessKeyId = "..."; o.SecretAccessKey = "...";
});
```

### Vector Store Options

```csharp
// Qdrant
services.AddBipinsAI().AddQdrant(o =>
{
    o.Endpoint = "http://localhost:6333";
    o.CollectionName = "documents";
    o.VectorSize = 1536;
});

// Pinecone
services.AddBipinsAI().AddPinecone(o =>
{
    o.ApiKey = "your-key";
    o.Environment = "us-east-1";
    o.IndexName = "my-index";
});

// Weaviate
services.AddBipinsAI().AddWeaviate(o =>
{
    o.Endpoint = "http://localhost:8080";
    o.ApiKey = "your-key";
    o.ClassName = "Document";
});

// Milvus
services.AddBipinsAI().AddMilvus(o =>
{
    o.Endpoint = "http://localhost:19530";
    o.CollectionName = "documents";
    o.VectorSize = 1536;
});
```

### Runtime Services

```csharp
// Requires IDistributedCache to be registered (e.g., AddStackExchangeRedisCache, AddDistributedMemoryCache)
services
    .AddDistributedMemoryCache() // or AddStackExchangeRedisCache(...)
    .AddBipinsAI()
    .AddBipinsAIRuntime(configuration);

// Available services:
// - ICache: Distributed caching wrapper
// - IRateLimiter: Rate limiting
// - ICostTracker: Cost tracking
// - IAiPolicyProvider: Policy management
```

## Core Types

### Models (`Bipins.AI.Core.Models`)

- `ChatRequest`, `ChatResponse`, `ChatResponseChunk`
- `Message`, `MessageRole` (enum)
- `ToolDefinition`, `ToolCall`
- `EmbeddingRequest`, `EmbeddingResponse`
- `Usage`, `SafetyInfo`
- `StructuredOutputOptions`

### Vector (`Bipins.AI.Vector`)

- `IVectorStore`, `VectorRecord`, `VectorMatch`
- `VectorQueryRequest`, `VectorQueryResponse`
- `VectorUpsertRequest`, `VectorDeleteRequest`
- `VectorFilter`, `VectorFilterBuilder`
- `FilterPredicate`, `FilterOperator` (enum)

### Ingestion (`Bipins.AI.Core.Ingestion`)

- `Document`, `Chunk`, `IndexResult`, `IndexOptions`
- `ChunkOptions`, `ChunkStrategy` (enum), `UpdateMode` (enum)
- `IDocumentLoader`, `IChunker`, `IIndexer`, `IMetadataEnricher`

### RAG (`Bipins.AI.Core.Rag`)

- `RetrieveRequest`, `RetrieveResult`, `RagChunk`
- `IRetriever`, `IRagComposer`

### Providers (`Bipins.AI.Providers`)

- `ILLMProvider`: Unified provider interface
- `IChatService`, `ChatService`: High-level chat API
- Provider-specific options and exceptions

### Utilities

- `SnakeCaseLowerNamingPolicy`: JSON naming policy for snake_case serialization
- `StructuredOutputHelper`: Helper for parsing structured JSON responses
- `ConfigurationExtensions`: Configuration helpers for environment variables

## Advanced Usage

### Custom Chunking Strategies

```csharp
services.AddBipinsAIIngestion();

// Available strategies:
// - FixedSizeChunkingStrategy
// - SentenceAwareChunkingStrategy
// - ParagraphChunkingStrategy
// - MarkdownAwareChunkingStrategy

services.AddSingleton<IChunker, MarkdownAwareChunker>();
```

### Vector Filtering

```csharp
var filter = VectorFilterBuilder
    .Create()
    .Equal("category", "technical")
    .AndGroup(b => b
        .GreaterThan("date", "2024-01-01")
        .LessThan("date", "2024-12-31"))
    .OrGroup(b => b
        .Equal("status", "published")
        .Equal("status", "reviewed"))
    .NotEqual("deleted", true)
    .Build();
```

### Rate Limiting

```csharp
var rateLimiter = serviceProvider.GetRequiredService<IRateLimiter>();

if (await rateLimiter.TryAcquireAsync("user123", 10, TimeSpan.FromMinutes(1)))
{
    // Proceed with request
}
```

### Cost Tracking

```csharp
var costTracker = serviceProvider.GetRequiredService<ICostTracker>();

await costTracker.RecordAsync(new CostRecord(
    Provider: "OpenAI",
    ModelId: "gpt-4",
    PromptTokens: 100,
    CompletionTokens: 50,
    Cost: 0.03m));

var totalCost = await costTracker.GetTotalCostAsync("OpenAI", DateTime.UtcNow.AddDays(-30));
```

## License

MIT License

## Project

[GitHub Repository](https://github.com/bipindra/Bipins.AI)
