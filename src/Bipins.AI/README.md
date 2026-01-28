# Bipins.AI

**Enterprise AI platform for building intelligent applications with RAG (Retrieval-Augmented Generation), multiple LLM providers, and vector databases.**

[![NuGet](https://img.shields.io/nuget/v/Bipins.AI.svg)](https://www.nuget.org/packages/Bipins.AI)
[![.NET](https://img.shields.io/badge/.NET-Standard%202.1%20%7C%207.0%20%7C%208.0%20%7C%209.0%20%7C%2010.0-purple.svg)](https://dotnet.microsoft.com/download)

## Features

- 🤖 **Multiple LLM Providers**: OpenAI, Anthropic Claude, Azure OpenAI, AWS Bedrock
- 🔍 **Vector Database Support**: Qdrant, Pinecone, Weaviate, Milvus
- 📚 **RAG (Retrieval-Augmented Generation)**: Built-in support for document ingestion and retrieval
- ⚡ **Runtime Services**: Caching, rate limiting, cost tracking, observability
- 🔧 **Easy Integration**: Simple dependency injection setup
- 🚀 **Production Ready**: Enterprise-grade features with distributed caching and rate limiting

## Installation

Install the package via NuGet Package Manager or .NET CLI:

```bash
dotnet add package Bipins.AI
```

## Quick Start

### 1. Basic Setup with OpenAI

```csharp
using Bipins.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices((context, services) =>
{
    // Add Bipins.AI with OpenAI provider
    services
        .AddBipinsAI()
        .AddOpenAI(o =>
        {
            o.ApiKey = context.Configuration["OpenAI:ApiKey"] 
                      ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY") 
                      ?? "your-api-key-here";
            o.DefaultChatModelId = "gpt-4";
            o.DefaultEmbeddingModelId = "text-embedding-3-small";
        });
});

var host = builder.Build();
```

### 2. Using Chat Models

```csharp
using Bipins.AI.Core.Models;
using Microsoft.Extensions.DependencyInjection;

// Inject IChatModel
var chatModel = serviceProvider.GetRequiredService<IChatModel>();

// Create a chat request
var request = new ChatRequest
{
    Messages = new List<Message>
    {
        new Message { Role = MessageRole.User, Content = "What is machine learning?" }
    },
    ModelId = "gpt-4"
};

// Generate response
var response = await chatModel.GenerateAsync(request);

Console.WriteLine(response.Content);
Console.WriteLine($"Tokens used: {response.Usage?.TotalTokens}");
```

### 3. Streaming Responses

```csharp
using Bipins.AI.Core.Models;

var streamingModel = serviceProvider.GetRequiredService<IChatModelStreaming>();

var request = new ChatRequest
{
    Messages = new List<Message>
    {
        new Message { Role = MessageRole.User, Content = "Tell me a story" }
    }
};

await foreach (var chunk in streamingModel.GenerateStreamAsync(request))
{
    Console.Write(chunk.Content);
}
```

### 4. Vector Store Integration

```csharp
using Bipins.AI;
using Bipins.AI.Core.Vector;

// Add Qdrant vector store
services
    .AddBipinsAI()
    .AddQdrant(o =>
    {
        o.Endpoint = "http://localhost:6333";
        o.CollectionName = "my-collection";
        o.VectorSize = 1536;
    });

// Use vector store
var vectorStore = serviceProvider.GetRequiredService<IVectorStore>();

// Upsert vectors
var records = new List<VectorRecord>
{
    new VectorRecord
    {
        Id = "doc1",
        Vector = new float[1536], // Your embedding vector
        Metadata = new Dictionary<string, object>
        {
            { "text", "Machine learning is..." },
            { "source", "document1.pdf" }
        }
    }
};

await vectorStore.UpsertAsync(new VectorUpsertRequest
{
    CollectionName = "my-collection",
    Records = records
});

// Query vectors
var queryResult = await vectorStore.QueryAsync(new VectorQueryRequest
{
    CollectionName = "my-collection",
    QueryVector = new float[1536], // Your query embedding
    TopK = 5
});

foreach (var match in queryResult.Matches)
{
    Console.WriteLine($"Score: {match.Score}, ID: {match.Id}");
}
```

### 5. RAG (Retrieval-Augmented Generation)

```csharp
using Bipins.AI;
using Bipins.AI.Core.Rag;

// Add RAG services
services
    .AddBipinsAI()
    .AddOpenAI(/* ... */)
    .AddQdrant(/* ... */)
    .AddBipinsAIRag();

// Use RAG composer
var ragComposer = serviceProvider.GetRequiredService<IRagComposer>();
var retriever = serviceProvider.GetRequiredService<IRetriever>();

// Retrieve relevant context
var retrieveResult = await retriever.RetrieveAsync(new RetrieveRequest
{
    Query = "What is machine learning?",
    TopK = 5
});

// Compose RAG response
var ragResult = await ragComposer.ComposeAsync(
    query: "What is machine learning?",
    retrievedChunks: retrieveResult.Chunks,
    chatModel: chatModel
);

Console.WriteLine(ragResult.Content);
foreach (var citation in ragResult.Citations)
{
    Console.WriteLine($"Source: {citation.SourceUri}");
}
```

### 6. Document Ingestion

```csharp
using Bipins.AI;
using Bipins.AI.Core.Ingestion;

// Add ingestion services
services
    .AddBipinsAI()
    .AddBipinsAIIngestion()
    .AddOpenAI(/* ... */)
    .AddQdrant(/* ... */);

// Use ingestion pipeline
var ingestionPipeline = serviceProvider.GetRequiredService<IngestionPipeline>();

var result = await ingestionPipeline.ProcessAsync(new Document
{
    Id = "doc1",
    Content = "Your document content here...",
    Metadata = new Dictionary<string, object>
    {
        { "title", "Introduction to AI" },
        { "author", "John Doe" }
    }
});

Console.WriteLine($"Chunks created: {result.Chunks.Count}");
```

### 7. Runtime Services

```csharp
using Bipins.AI;
using Microsoft.Extensions.Configuration;

// Add runtime services (caching, rate limiting, cost tracking)
services
    .AddBipinsAI()
    .AddBipinsAIRuntime(configuration); // Pass IConfiguration for Redis support

// Runtime services are automatically available:
// - ICache (Memory or Redis-based)
// - IRateLimiter (Memory or Distributed)
// - ICostTracker (tracks API usage and costs)
// - IAiPolicyProvider (policies for retries, throttling, etc.)
```

## Configuration

### appsettings.json

```json
{
  "OpenAI": {
    "ApiKey": "your-api-key-here",
    "BaseUrl": "https://api.openai.com/v1",
    "DefaultChatModelId": "gpt-4",
    "DefaultEmbeddingModelId": "text-embedding-3-small"
  },
  "Anthropic": {
    "ApiKey": "your-api-key-here",
    "DefaultModelId": "claude-3-opus-20240229"
  },
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com",
    "ApiKey": "your-api-key-here",
    "DeploymentName": "gpt-4"
  },
  "Bedrock": {
    "Region": "us-east-1",
    "DefaultModelId": "anthropic.claude-3-opus-20240229-v1:0"
  },
  "Qdrant": {
    "Endpoint": "http://localhost:6333",
    "CollectionName": "default",
    "VectorSize": 1536
  },
  "Pinecone": {
    "ApiKey": "your-api-key-here",
    "Environment": "us-east-1",
    "IndexName": "my-index"
  },
  "Cache": {
    "DefaultTtlHours": 24,
    "KeyPrefix": "bipins:cache:"
  },
  "ConnectionStrings": {
    "Redis": "localhost:6379" // Optional: for distributed caching and rate limiting
  }
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

## Advanced Usage

### Custom Chunking Strategies

```csharp
services.AddBipinsAIIngestion();

// Use different chunking strategies
services.AddSingleton<IChunker, FixedSizeChunkingStrategy>();
// or
services.AddSingleton<IChunker, SentenceAwareChunkingStrategy>();
```

### Rate Limiting

```csharp
var rateLimiter = serviceProvider.GetRequiredService<IRateLimiter>();

if (await rateLimiter.TryAcquireAsync("user123", 10, TimeSpan.FromMinutes(1)))
{
    // Proceed with request
}
else
{
    // Rate limit exceeded
}
```

### Cost Tracking

```csharp
var costTracker = serviceProvider.GetRequiredService<ICostTracker>();

await costTracker.RecordAsync(new CostRecord
{
    Provider = "OpenAI",
    ModelId = "gpt-4",
    PromptTokens = 100,
    CompletionTokens = 50,
    Cost = 0.03m
});

var totalCost = await costTracker.GetTotalCostAsync("OpenAI", DateTime.UtcNow.AddDays(-30));
```

## Examples

Check out the [samples directory](https://github.com/bipindra/Bipins.AI/tree/main/samples) for complete examples:

- **Bipins.AI.Samples**: Basic usage examples
- **AICostOptimizationAdvisor**: AWS cost optimization with Bedrock
- **AICloudCostOptimizationAdvisor**: Multi-cloud cost analysis with Terraform

## Documentation

- [Getting Started Guide](https://github.com/bipindra/Bipins.AI/blob/main/docs/GETTING_STARTED.md)
- [API Reference](https://github.com/bipindra/Bipins.AI/blob/main/docs/API_REFERENCE.md)
- [Architecture Overview](https://github.com/bipindra/Bipins.AI/blob/main/README.md#architecture)

## License

MIT License - see [LICENSE](https://github.com/bipindra/Bipins.AI/blob/main/LICENSE) file for details.

## Contributing

Contributions are welcome! Please see our [Contributing Guide](https://github.com/bipindra/Bipins.AI/blob/main/CONTRIBUTING.md) for details.

## Support

- **Issues**: [GitHub Issues](https://github.com/bipindra/Bipins.AI/issues)
- **Discussions**: [GitHub Discussions](https://github.com/bipindra/Bipins.AI/discussions)
