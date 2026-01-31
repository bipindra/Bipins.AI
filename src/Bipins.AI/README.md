# Bipins.AI

Enterprise AI platform for building intelligent applications with RAG (Retrieval-Augmented Generation), multiple LLM providers, vector databases, and Agentic AI capabilities.

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

// Custom tools
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

### Agents (`Bipins.AI.Agents`)

- `IAgent`: Core agent interface for autonomous agent execution
- `AgentRequest`, `AgentResponse`, `AgentResponseChunk`: Agent request/response models
- `AgentOptions`, `AgentCapabilities` (enum), `AgentStatus` (enum): Agent configuration
- `AgentExecutionPlan`, `PlanStep`: Planning structures
- `IAgentRegistry`, `DefaultAgentRegistry`: Agent registration and discovery
- `BaseAgent`, `DefaultAgent`: Agent implementations

### Agent Tools (`Bipins.AI.Agents.Tools`)

- `IToolExecutor`: Interface for tool implementations
- `IToolRegistry`, `DefaultToolRegistry`: Tool registration and discovery
- `ToolExecutionResult`: Tool execution result model
- Built-in tools: `CalculatorTool`, `VectorSearchTool`

### Agent Memory (`Bipins.AI.Agents.Memory`)

- `IAgentMemory`: Interface for conversation memory
- `InMemoryAgentMemory`: In-memory implementation
- `VectorStoreAgentMemory`: Vector store-based memory with semantic search
- `AgentMemoryContext`, `AgentMemoryEntry`: Memory context models

### Agent Planning (`Bipins.AI.Agents.Planning`)

- `IAgentPlanner`: Interface for execution planning
- `LLMPlanner`: LLM-based planner using structured output
- `NoOpPlanner`: Simple fallback planner

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
