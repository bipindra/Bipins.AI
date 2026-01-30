# Bipins.AI

Enterprise AI platform for building intelligent applications with RAG (Retrieval-Augmented Generation), multiple LLM providers, and vector databases.

[![NuGet](https://img.shields.io/nuget/v/Bipins.AI.svg)](https://www.nuget.org/packages/Bipins.AI)
[![.NET](https://img.shields.io/badge/.NET-Standard%202.1%20%7C%207.0%20%7C%208.0%20%7C%209.0%20%7C%2010.0-purple.svg)](https://dotnet.microsoft.com/download)

## Installation

```bash
dotnet add package Bipins.AI
```

## Features

- **Multi-Provider LLM Support**: OpenAI, Azure OpenAI, Anthropic Claude, AWS Bedrock
- **Vector Database Integration**: Qdrant, Pinecone, Weaviate, Milvus
- **RAG (Retrieval-Augmented Generation)**: Built-in document ingestion, chunking, retrieval, and composition
- **Streaming Support**: Async enumerable streaming for chat completions
- **Function Calling / Tools**: Native support for tool definitions and tool calls
- **Structured Output**: JSON schema validation and parsing
- **Multi-Tenant Isolation**: Tenant-based data isolation and quota management
- **Document Versioning**: Support for document versioning and update modes
- **Chunking Strategies**: Fixed-size, sentence-aware, paragraph, and markdown-aware chunking
- **Metadata Filtering**: Advanced vector query filtering with predicate builders
- **Rate Limiting & Throttling**: Built-in rate limiting and throttling policies
- **Cost Tracking**: Token usage and cost calculation across providers
- **Caching**: Distributed cache support via `IDistributedCache`
- **Observability**: OpenTelemetry integration for distributed tracing

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
        .AddBipinsAIRag()
        .AddQdrant(o =>
        {
            o.Endpoint = context.Configuration["Qdrant:Endpoint"] 
                        ?? Environment.GetEnvironmentVariable("QDRANT_ENDPOINT") 
                        ?? "http://localhost:6333";
            o.DefaultCollectionName = "documents";
            o.VectorSize = 1536;
            o.CreateCollectionIfMissing = true;
        });
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
var tools = new List<ToolDefinition>
{
    new ToolDefinition(
        Type: "function",
        Function: new FunctionDefinition(
            Name: "get_weather",
            Description: "Get the current weather in a given location",
            Parameters: new
            {
                type = "object",
                properties = new
                {
                    location = new { type = "string", description = "The city and state, e.g. San Francisco, CA" },
                    unit = new { type = "string", @enum = new[] { "celsius", "fahrenheit" } }
                },
                required = new[] { "location" }
            }
        )
    )
};

var request = new ChatRequest(
    Messages: new[] { new Message(MessageRole.User, "What's the weather in San Francisco?") },
    Tools: tools);

var response = await chatModel.GenerateAsync(request);

if (response.ToolCalls != null && response.ToolCalls.Count > 0)
{
    foreach (var toolCall in response.ToolCalls)
    {
        Console.WriteLine($"Tool: {toolCall.Function.Name}");
        Console.WriteLine($"Arguments: {toolCall.Function.Arguments}");
    }
}
```

### Embeddings

```csharp
var embeddingModel = serviceProvider.GetRequiredService<IEmbeddingModel>();

var embeddingRequest = new EmbeddingRequest(
    Input: "Your text to embed",
    ModelId: "text-embedding-3-small");

var embedding = await embeddingModel.EmbedAsync(embeddingRequest);
Console.WriteLine($"Embedding dimension: {embedding.Data[0].Embedding.Length}");
```

### Document Ingestion

```csharp
var pipeline = serviceProvider.GetRequiredService<IngestionPipeline>();

var options = new IndexOptions(
    tenantId: "tenant1",
    docId: "doc1",
    versionId: "v1.0.0",
    collectionName: "documents",
    chunkStrategy: ChunkStrategy.FixedSize,
    chunkOptions: new ChunkOptions(
        maxChunkSize: 1000,
        overlap: 200));

var result = await pipeline.IngestAsync("path/to/document.md", options);
Console.WriteLine($"Indexed {result.ChunksIndexed} chunks");
```

### RAG (Retrieval-Augmented Generation)

```csharp
var retriever = serviceProvider.GetRequiredService<IRetriever>();
var composer = serviceProvider.GetRequiredService<IRagComposer>();
var chatModel = serviceProvider.GetRequiredService<IChatModel>();

// Retrieve relevant chunks
var retrieveRequest = new RetrieveRequest(
    query: "What is machine learning?",
    tenantId: "tenant1",
    topK: 5);

var retrieved = await retriever.RetrieveAsync(retrieveRequest);

// Compose augmented request
var chatRequest = new ChatRequest(
    Messages: new[] { new Message(MessageRole.User, "What is machine learning?") });

var augmentedRequest = composer.Compose(chatRequest, retrieved);

// Generate response
var response = await chatModel.GenerateAsync(augmentedRequest);
Console.WriteLine(response.Content);
```

### Vector Store Operations

```csharp
var vectorStore = serviceProvider.GetRequiredService<IVectorStore>();

// Upsert vectors
var upsertRequest = new VectorUpsertRequest(
    Records: new[]
    {
        new VectorRecord(
            Id: "doc1",
            Vector: new ReadOnlyMemory<float>(new float[] { 0.1f, 0.2f, 0.3f }),
            Text: "Sample document text",
            Metadata: new Dictionary<string, object> { ["source"] = "test" },
            TenantId: "tenant1",
            VersionId: "v1")
    },
    CollectionName: "documents");

await vectorStore.UpsertAsync(upsertRequest);

// Query vectors
var queryRequest = new VectorQueryRequest(
    QueryVector: new ReadOnlyMemory<float>(new float[] { 0.1f, 0.2f, 0.3f }),
    TopK: 5,
    TenantId: "tenant1",
    CollectionName: "documents",
    Filter: new VectorFilterBuilder()
        .And("source", FilterOperator.Equal, "test")
        .Build());

var results = await vectorStore.QueryAsync(queryRequest);

foreach (var match in results.Matches)
{
    Console.WriteLine($"ID: {match.Id}, Score: {match.Score}, Text: {match.Text}");
}
```

## Supported Providers

### LLM Providers

- **OpenAI**: GPT-3.5, GPT-4, GPT-4 Turbo, and embedding models
- **Azure OpenAI**: Full compatibility with Azure-hosted OpenAI models
- **Anthropic**: Claude 3 (Opus, Sonnet, Haiku) and streaming support
- **AWS Bedrock**: Amazon Bedrock models (Claude, Llama, Titan)

### Vector Stores

- **Qdrant**: Self-hosted or cloud Qdrant instances
- **Pinecone**: Pinecone cloud vector database
- **Weaviate**: Weaviate open-source vector database
- **Milvus**: Milvus vector database

## Configuration

### Provider Configuration

```csharp
// OpenAI
services.AddBipinsAI().AddOpenAI(o =>
{
    o.ApiKey = "your-api-key";
    o.DefaultChatModelId = "gpt-4";
    o.DefaultEmbeddingModelId = "text-embedding-3-small";
});

// Azure OpenAI
services.AddBipinsAI().AddAzureOpenAI(o =>
{
    o.Endpoint = "https://your-resource.openai.azure.com";
    o.ApiKey = "your-api-key";
    o.DeploymentName = "gpt-4";
    o.EmbeddingDeploymentName = "text-embedding-3-small";
});

// Anthropic
services.AddBipinsAI().AddAnthropic(o =>
{
    o.ApiKey = "your-api-key";
    o.DefaultModelId = "claude-3-opus-20240229";
});

// AWS Bedrock
services.AddBipinsAI().AddBedrock(o =>
{
    o.Region = "us-east-1";
    o.DefaultModelId = "anthropic.claude-3-opus-20240229-v1:0";
});
```

### Vector Store Configuration

```csharp
// Qdrant
services.AddBipinsAI().AddQdrant(o =>
{
    o.Endpoint = "http://localhost:6333";
    o.DefaultCollectionName = "documents";
    o.VectorSize = 1536;
    o.CreateCollectionIfMissing = true;
});

// Pinecone
services.AddBipinsAI().AddPinecone(o =>
{
    o.ApiKey = "your-api-key";
    o.Environment = "us-west1-gcp";
    o.IndexName = "documents";
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
// Requires IDistributedCache to be registered
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
- `ToolDefinition`, `ToolCall`, `FunctionDefinition`
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
- `IChunkingStrategy`, `IChunkingStrategyFactory`

### RAG (`Bipins.AI.Core.Rag`)

- `RetrieveRequest`, `RetrieveResult`, `RagChunk`
- `IRetriever`, `IRagComposer`

### Providers (`Bipins.AI.Providers`)

- `ILLMProvider`: Unified provider interface for chat and embeddings
- `IChatService`, `ChatService`: High-level chat API
- `IChatModel`, `IChatModelStreaming`: Chat model interfaces
- `IEmbeddingModel`: Embedding model interface

### Utilities

- `SnakeCaseLowerNamingPolicy`: JSON naming policy for snake_case serialization
- `StructuredOutputHelper`: Helper for parsing structured JSON responses

## Requirements

- .NET Standard 2.1, .NET 7.0, .NET 8.0, .NET 9.0, or .NET 10.0
- For vector stores: Qdrant, Pinecone, Weaviate, or Milvus instance
- For LLM providers: API keys for respective providers

## License

MIT License

Copyright (c) 2026 Bipins

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
