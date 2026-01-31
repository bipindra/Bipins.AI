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
- **Agentic AI**: Autonomous agents with tool execution, planning, and memory
- **Content Moderation**: Built-in content moderation with Azure Cognitive Services support
- **Validation**: FluentValidation and JSON Schema validation frameworks
- **Resilience**: Polly-based retry, timeout, circuit breaker, and bulkhead policies

The library includes comprehensive sample applications demonstrating RAG workflows, cost optimization analysis, and serverless architectures. See the [Samples](#samples) section for details.

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
using System.Text.Json;

var tools = new List<ToolDefinition>
{
    new ToolDefinition(
        Name: "get_weather",
        Description: "Get the current weather in a given location",
        Parameters: JsonSerializer.SerializeToElement(new
        {
            type = "object",
            properties = new
            {
                location = new { type = "string", description = "The city and state, e.g. San Francisco, CA" },
                unit = new { type = "string", @enum = new[] { "celsius", "fahrenheit" } }
            },
            required = new[] { "location" }
        })
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
        Console.WriteLine($"Tool: {toolCall.Name}");
        Console.WriteLine($"Arguments: {toolCall.Arguments}");
    }
}
```

### Embeddings

```csharp
var embeddingModel = serviceProvider.GetRequiredService<IEmbeddingModel>();

var embeddingRequest = new EmbeddingRequest(
    Inputs: new[] { "Your text to embed" },
    ModelId: "text-embedding-3-small");

var embedding = await embeddingModel.EmbedAsync(embeddingRequest);
Console.WriteLine($"Embedding dimension: {embedding.Vectors[0].Length}");
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
        .Equal("source", "test")
        .Build());

var results = await vectorStore.QueryAsync(queryRequest);

foreach (var match in results.Matches)
{
    Console.WriteLine($"ID: {match.Record.Id}, Score: {match.Score}, Text: {match.Record.Text}");
}
```

### Agentic AI

```csharp
using Bipins.AI.Agents;
using Bipins.AI.Agents.Tools;

// Register agent support
services
    .AddBipinsAI()
    .AddOpenAI(o => { /* ... */ })
    .AddBipinsAIAgents()
    .AddCalculatorTool()
    .AddVectorSearchTool("documents")
    .AddAgent("assistant", options =>
    {
        options.Name = "AI Assistant";
        options.SystemPrompt = "You are a helpful AI assistant that can use tools to help users.";
        options.EnablePlanning = true;
        options.EnableMemory = true;
        options.MaxIterations = 10;
        options.Temperature = 0.7f;
    });

// Use an agent
var agentRegistry = serviceProvider.GetRequiredService<IAgentRegistry>();
var agent = agentRegistry.GetAgent("assistant");

var request = new AgentRequest(
    Goal: "Calculate 15 * 23 and then search for information about machine learning",
    Context: "User wants mathematical calculation and research",
    SessionId: "session-123");

var response = await agent.ExecuteAsync(request);
Console.WriteLine($"Response: {response.Content}");
Console.WriteLine($"Status: {response.Status}");
Console.WriteLine($"Iterations: {response.Iterations}");

// Streaming agent execution
await foreach (var chunk in agent.ExecuteStreamAsync(request))
{
    Console.Write(chunk.Content);
    if (chunk.IsComplete)
    {
        Console.WriteLine($"\nStatus: {chunk.Status}");
    }
}
```

### Custom Tools

```csharp
// Implement a custom tool
public class WeatherTool : IToolExecutor
{
    public string Name => "get_weather";
    public string Description => "Gets the current weather for a location";
    public JsonElement ParametersSchema => JsonSerializer.SerializeToElement(new
    {
        type = "object",
        properties = new
        {
            location = new { type = "string", description = "City name" }
        },
        required = new[] { "location" }
    });

    public async Task<ToolExecutionResult> ExecuteAsync(ToolCall toolCall, CancellationToken cancellationToken)
    {
        var location = toolCall.Arguments.GetProperty("location").GetString();
        // Implement weather API call
        var weather = await GetWeatherAsync(location);
        return new ToolExecutionResult(Success: true, Result: weather);
    }
}

// Register custom tool
services
    .AddBipinsAI()
    .AddBipinsAIAgents()
    .AddTool(new WeatherTool());
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

### Agent Configuration

```csharp
// Basic agent setup
services
    .AddBipinsAI()
    .AddOpenAI(o => { /* ... */ })
    .AddBipinsAIAgents()
    .AddAgent("assistant", options =>
    {
        options.Name = "AI Assistant";
        options.SystemPrompt = "You are a helpful assistant.";
        options.EnablePlanning = true;
        options.EnableMemory = true;
        options.MaxIterations = 10;
        options.Temperature = 0.7f;
    });

// Use vector store for agent memory
services
    .AddBipinsAI()
    .AddQdrant(o => { /* ... */ })
    .AddBipinsAIAgents()
    .UseVectorStoreMemory("agent_memory")
    .AddAgent("assistant", options => { /* ... */ });

// Register built-in tools
services
    .AddBipinsAI()
    .AddBipinsAIAgents()
    .AddCalculatorTool()
    .AddVectorSearchTool("documents");

// Available agent services:
// - IAgent: Individual agent instances
// - IAgentRegistry: Agent registry for discovery
// - IToolRegistry: Tool registry
// - IAgentMemory: Agent memory (default: InMemoryAgentMemory)
// - IAgentPlanner: Agent planner (default: LLMPlanner)
```

### Content Moderation

```csharp
using Bipins.AI.Safety;
using Bipins.AI.Safety.Azure;

// Add content moderation
services
    .AddBipinsAI()
    .AddOpenAI(o => { /* ... */ })
    .AddContentModeration(options =>
    {
        options.Enabled = true;
        options.MinimumSeverityToBlock = SafetySeverity.High;
        options.FilterUnsafeContent = false;
        options.ThrowOnUnsafeContent = false;
        options.BlockedCategories = new List<SafetyCategory> 
        { 
            SafetyCategory.PromptInjection, 
            SafetyCategory.SelfHarm 
        };
    })
    .AddAzureContentModerator(azureOptions =>
    {
        azureOptions.Endpoint = "https://your-region.api.cognitive.microsoft.com";
        azureOptions.SubscriptionKey = "your-key";
        azureOptions.DetectPII = true;
    })
    .UseContentModerationMiddleware();

// Content moderation is automatically applied to all LLM requests and responses
var llmProvider = serviceProvider.GetRequiredService<ILLMProvider>();
var response = await llmProvider.ChatAsync(new ChatRequest(
    Messages: new[] { new Message(MessageRole.User, "Hello") }));

// Check safety info
if (response.Safety?.Flagged == true)
{
    Console.WriteLine($"Content flagged: {string.Join(", ", response.Safety.Categories?.Keys ?? Array.Empty<string>())}");
}
```

### Validation

```csharp
using Bipins.AI.Validation;
using Bipins.AI.Validation.FluentValidation;
using Bipins.AI.Validation.JsonSchema;
using FluentValidation;

// Add validation framework
services
    .AddBipinsAI()
    .AddValidation()
    .AddFluentValidation()
    .AddJsonSchemaValidation();

// FluentValidation for request validation
public class ChatRequestValidator : AbstractValidator<ChatRequest>
{
    public ChatRequestValidator()
    {
        RuleFor(x => x.Messages)
            .NotEmpty()
            .Must(m => m.Any(msg => msg.Role == MessageRole.User))
            .WithMessage("At least one user message is required");
    }
}

services.AddValidatorsFromAssemblyContaining<ChatRequestValidator>();

// Use request validator
var requestValidator = serviceProvider.GetRequiredService<IRequestValidator<ChatRequest>>();
var validationResult = await requestValidator.ValidateAsync(request);
if (!validationResult.IsValid)
{
    foreach (var error in validationResult.Errors)
    {
        Console.WriteLine($"{error.PropertyName}: {error.ErrorMessage}");
    }
}

// JSON Schema validation for responses
var responseValidator = serviceProvider.GetRequiredService<IResponseValidator<string>>();
var schema = @"{
    ""type"": ""object"",
    ""properties"": {
        ""content"": { ""type"": ""string"", ""minLength"": 1 }
    },
    ""required"": [""content""]
}";

var responseJson = JsonSerializer.Serialize(response);
var validationResult = await responseValidator.ValidateAsync(responseJson, schema);
```

### Resilience

```csharp
using Bipins.AI.Resilience;

// Add resilience with Polly
services
    .AddBipinsAI()
    .AddResilience(options =>
    {
        options.Retry = new RetryOptions
        {
            MaxRetries = 3,
            Delay = TimeSpan.FromSeconds(1),
            BackoffStrategy = BackoffStrategy.Exponential,
            MaxDelay = TimeSpan.FromSeconds(10)
        };
        options.Timeout = new TimeoutOptions
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        options.Bulkhead = new BulkheadOptions
        {
            MaxParallelization = 10,
            MaxQueuingActions = 5
        };
    });

// Use resilience policy
var resiliencePolicy = serviceProvider.GetRequiredService<IResiliencePolicy>();

var response = await resiliencePolicy.ExecuteAsync(async () =>
{
    return await llmProvider.ChatAsync(new ChatRequest(
        Messages: new[] { new Message(MessageRole.User, "Hello") }));
});
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

### Agents (`Bipins.AI.Agents`)

- `IAgent`: Core agent interface
- `AgentRequest`, `AgentResponse`, `AgentResponseChunk`
- `AgentOptions`, `AgentCapabilities` (enum), `AgentStatus` (enum)
- `AgentExecutionPlan`, `PlanStep`
- `IAgentRegistry`, `DefaultAgentRegistry`
- `BaseAgent`, `DefaultAgent`: Agent implementations

### Agent Tools (`Bipins.AI.Agents.Tools`)

- `IToolExecutor`: Interface for tool implementations
- `IToolRegistry`, `DefaultToolRegistry`: Tool registration and discovery
- `ToolExecutionResult`
- Built-in tools: `CalculatorTool`, `VectorSearchTool`

### Agent Memory (`Bipins.AI.Agents.Memory`)

- `IAgentMemory`: Interface for conversation memory
- `InMemoryAgentMemory`: In-memory implementation
- `VectorStoreAgentMemory`: Vector store-based memory with semantic search
- `AgentMemoryContext`, `AgentMemoryEntry`

### Agent Planning (`Bipins.AI.Agents.Planning`)

- `IAgentPlanner`: Interface for execution planning
- `LLMPlanner`: LLM-based planner using structured output
- `NoOpPlanner`: Simple fallback planner

### Safety (`Bipins.AI.Safety`)

- `IContentModerator`: Interface for content moderation services
- `ModerationResult`, `SafetyViolation`: Moderation result models
- `SafetyCategory` (enum), `SafetySeverity` (enum): Safety classification
- `ContentModerationOptions`: Configuration for content moderation
- `AzureContentModerator`: Azure Cognitive Services implementation
- `ILLMProviderMiddleware`: Service-level middleware interface
- `ContentModerationLLMMiddleware`: Middleware for applying content moderation
- `ModeratedLLMProvider`: Decorator that applies moderation middleware

### Resilience (`Bipins.AI.Resilience`)

- `IResiliencePolicy`: Interface for resilience policies
- `IResiliencePolicyFactory`: Factory for creating resilience policies
- `ResilienceOptions`: Configuration for resilience policies
- `RetryOptions`, `CircuitBreakerOptions`, `TimeoutOptions`, `BulkheadOptions`: Policy-specific options
- `BackoffStrategy` (enum): Retry backoff strategies
- `PollyResiliencePolicy`: Polly-based implementation

### Validation (`Bipins.AI.Validation`)

- `IRequestValidator<T>`, `IResponseValidator<T>`: Validation interfaces
- `ValidationResult`, `ValidationError`, `ValidationWarning`: Validation result models
- `ValidationOptions`: Configuration for validation framework
- `FluentValidationValidator<T>`: FluentValidation-based request validator
- `FluentValidationValidatorFactory`: Factory for FluentValidation validators
- `NJsonSchemaValidator<T>`, `JsonSchemaValidator`: JSON Schema-based response validators

### Utilities

- `SnakeCaseLowerNamingPolicy`: JSON naming policy for snake_case serialization
- `StructuredOutputHelper`: Helper for parsing structured JSON responses

## Requirements

- .NET Standard 2.1, .NET 7.0, .NET 8.0, .NET 9.0, or .NET 10.0
- For vector stores: Qdrant, Pinecone, Weaviate, or Milvus instance
- For LLM providers: API keys for respective providers

## Samples

The repository includes comprehensive sample applications demonstrating various use cases and integrations with Bipins.AI. These samples showcase real-world implementations including RAG workflows, cost optimization analysis, and serverless architectures.

a) **Bipins.AI.Samples** - A console application demonstrating core RAG (Retrieval-Augmented Generation) capabilities. This sample shows how to ingest documents, perform vector-based retrieval, and compose augmented chat requests. It includes examples of document loading, chunking strategies, embedding generation, and querying with citations.

b) **AICloudCostOptimizationAdvisor** - A web application that analyzes Terraform infrastructure scripts to provide AI-powered cost optimization and security risk assessment. Built with ASP.NET Core MVC, it demonstrates multi-cloud cost analysis (AWS, Azure, GCP), security vulnerability detection, compliance framework mapping, and interactive visualization of architecture improvements and cost breakdowns.

c) **AICostOptimizationAdvisor** - A serverless application built with AWS Lambda and React that analyzes AWS Cost Explorer data using AWS Bedrock. This sample demonstrates serverless architecture patterns, integration with AWS services, cost data caching with DynamoDB, and AI-powered cost analysis with historical tracking capabilities.

d) **Bipins.AI.AgentSamples** - An interactive console application demonstrating Agentic AI capabilities with autonomous agents. This sample showcases agent execution with multiple tools (calculator, weather, vector search), conversation memory across multiple requests, LLM-based planning for complex multi-step tasks, streaming agent responses, and RAG integration with vector search. The application features a modular architecture with an interactive menu system for selecting and running different scenarios. It demonstrates how agents can autonomously use tools, maintain context across conversations, plan multi-step workflows, and provide real-time streaming responses.

e) **Bipins.AI.Guardian** - A simple MVC web application demonstrating safety, validation, and resilience features. This sample showcases content moderation with automatic detection of unsafe content, FluentValidation for request validation, JSON Schema validation for response structure, and Polly-based resilience policies with retry and timeout handling. The application provides a clean web interface for testing these features with OpenAI chat completions, displaying moderation status, validation results, and retry information.

e) **Bipins.AI.Guardian** - A simple MVC web application demonstrating safety, validation, and resilience features. This sample showcases content moderation with automatic detection of unsafe content, FluentValidation for request validation, JSON Schema validation for response structure, and Polly-based resilience policies with retry and timeout handling. The application provides a clean web interface for testing these features with OpenAI chat completions, displaying moderation status, validation results, and retry information.

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
