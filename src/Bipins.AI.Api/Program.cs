using System.Diagnostics;
using System.Text.Json;
using Bipins.AI.Api.Authentication;
using Bipins.AI.Core;
using Bipins.AI.Core.Configuration;
using Bipins.AI.Core.DependencyInjection;
using Bipins.AI.Core.Contracts;
using Bipins.AI.Core.Ingestion;
using Bipins.AI.Core.Models;
using Bipins.AI.Core.Rag;
using Bipins.AI.Connectors.Llm.OpenAI;
using Bipins.AI.Connectors.Vector.Qdrant;
using Bipins.AI.Ingestion;
using Bipins.AI.Runtime;
using Bipins.AI.Api.Middleware;
using Bipins.AI.Runtime.Observability;
using Bipins.AI.Runtime.Policies;
using Bipins.AI.Runtime.Rag;
using Bipins.AI.Runtime.Routing;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add user secrets support
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Bipins.AI services
builder.Services.AddBipinsAI();
builder.Services.AddBipinsAIRuntime(builder.Configuration);
builder.Services.AddBipinsAIIngestion();
builder.Services.AddBipinsAIRag();

// Configure rate limiting
builder.Services.Configure<Bipins.AI.Runtime.Policies.RateLimitingOptions>(options =>
{
    options.MaxConcurrentRequests = builder.Configuration.GetValue<int>("RateLimiting:MaxConcurrentRequests", 10);
    options.MaxRequestsPerWindow = builder.Configuration.GetValue<int>("RateLimiting:MaxRequestsPerWindow", 100);
    var timeWindowMinutes = builder.Configuration.GetValue<int>("RateLimiting:TimeWindowMinutes", 1);
    options.TimeWindow = TimeSpan.FromMinutes(timeWindowMinutes);
});
builder.Services
    .AddBipinsAI()
    .AddOpenAI(o =>
    {
        o.ApiKey = builder.Configuration.GetRequiredValueOrEnvironmentVariable("OpenAI:ApiKey", "OPENAI_API_KEY");
        o.BaseUrl = builder.Configuration.GetValueOrEnvironmentVariable("OpenAI:BaseUrl", "OPENAI_BASE_URL") ?? "https://api.openai.com/v1";
        o.DefaultChatModelId = builder.Configuration.GetValueOrEnvironmentVariable("OpenAI:DefaultChatModelId", "OPENAI_DEFAULT_CHAT_MODEL_ID") ?? "gpt-3.5-turbo";
        o.DefaultEmbeddingModelId = builder.Configuration.GetValueOrEnvironmentVariable("OpenAI:DefaultEmbeddingModelId", "OPENAI_DEFAULT_EMBEDDING_MODEL_ID") ?? "text-embedding-ada-002";
    })
    .AddQdrant(o =>
    {
        o.Endpoint = builder.Configuration.GetValueOrEnvironmentVariable("Qdrant:Endpoint", "QDRANT_ENDPOINT") ?? "http://localhost:6333";
        o.ApiKey = builder.Configuration.GetValueOrEnvironmentVariable("Qdrant:ApiKey", "QDRANT_API_KEY");
        o.DefaultCollectionName = builder.Configuration.GetValueOrEnvironmentVariable("Qdrant:CollectionName", "QDRANT_COLLECTION_NAME") ?? "default";
        o.VectorSize = int.Parse(builder.Configuration.GetValueOrEnvironmentVariable("Qdrant:VectorSize", "QDRANT_VECTOR_SIZE") ?? "1536");
        o.CreateCollectionIfMissing = true;
    });

// Basic authentication (simplified for v1)
builder.Services.AddAuthentication("Basic")
    .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("Basic", null);

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

// Rate limiting middleware
app.UseMiddleware<Bipins.AI.Api.Middleware.RateLimitMiddleware>();

// Error handling middleware
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(new { error = ex.Message });
    }
});

// Endpoints
app.MapPost("/v1/ingest/text", async (
    HttpContext context,
    IngestionPipeline pipeline,
    IModelRouter router,
    JsonDocument request) =>
{
    var tenantId = context.User.FindFirst("tenantId")?.Value ?? "default";
    var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();

    var tenantIdFromBody = request.RootElement.TryGetProperty("tenantId", out var tenantProp)
        ? tenantProp.GetString()
        : null;
    tenantId = tenantIdFromBody ?? tenantId;

    var docId = request.RootElement.TryGetProperty("docId", out var docProp)
        ? docProp.GetString()
        : null;

    var sourceUri = request.RootElement.TryGetProperty("sourceUri", out var uriProp)
        ? uriProp.GetString()
        : null;

    var text = request.RootElement.TryGetProperty("text", out var textProp)
        ? textProp.GetString()
        : null;

    if (string.IsNullOrEmpty(text))
    {
        context.Response.StatusCode = 400;
        return Results.BadRequest(new { error = "text is required" });
    }

    // Create temporary file for ingestion
    var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.txt");
    await File.WriteAllTextAsync(tempFile, text);

    try
    {
        var versionId = request.RootElement.TryGetProperty("versionId", out var versionProp)
            ? versionProp.GetString()
            : null;
        
        var updateMode = request.RootElement.TryGetProperty("updateMode", out var modeProp)
            ? Enum.TryParse<UpdateMode>(modeProp.GetString(), out var mode) ? mode : UpdateMode.Upsert
            : UpdateMode.Upsert;
        
        var deleteOldVersions = request.RootElement.TryGetProperty("deleteOldVersions", out var deleteProp)
            ? deleteProp.GetBoolean()
            : false;

        var options = new IndexOptions(tenantId, docId, versionId, null, updateMode, deleteOldVersions);
        var result = await pipeline.IngestAsync(tempFile, options, cancellationToken: context.RequestAborted);

        var output = new AiOutputEnvelope(
            result.Errors?.Count > 0 ? OutputStatus.Partial : OutputStatus.Success,
            "ingest",
            JsonSerializer.SerializeToElement(result),
            null,
            null,
            result.Errors);

        return Results.Ok(output);
    }
    finally
    {
        if (File.Exists(tempFile))
        {
            File.Delete(tempFile);
        }
    }
})
.RequireAuthorization()
.WithName("IngestText")
.WithOpenApi();

app.MapPost("/v1/ingest/batch", async (
    HttpContext context,
    IngestionPipeline pipeline,
    IModelRouter router,
    JsonDocument request) =>
{
    var tenantId = context.User.FindFirst("tenantId")?.Value ?? "default";
    var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();

    using var activity = BipinsAiActivitySource.Instance.StartActivity("api.ingest.batch");

    try
    {
        var tenantIdFromBody = request.RootElement.TryGetProperty("tenantId", out var tenantProp)
            ? tenantProp.GetString()
            : null;
        tenantId = tenantIdFromBody ?? tenantId;

        var docIds = request.RootElement.TryGetProperty("docIds", out var docIdsProp)
            ? JsonSerializer.Deserialize<List<string>>(docIdsProp.GetRawText())
            : null;

        var sourceUris = request.RootElement.TryGetProperty("sourceUris", out var urisProp)
            ? JsonSerializer.Deserialize<List<string>>(urisProp.GetRawText())
            : null;

        var texts = request.RootElement.TryGetProperty("texts", out var textsProp)
            ? JsonSerializer.Deserialize<List<string>>(textsProp.GetRawText())
            : null;

        if (sourceUris == null && texts == null)
        {
            return Results.BadRequest(new { error = "Either sourceUris or texts must be provided" });
        }

        if (sourceUris != null && texts != null)
        {
            return Results.BadRequest(new { error = "Cannot provide both sourceUris and texts" });
        }

        var maxConcurrency = request.RootElement.TryGetProperty("maxConcurrency", out var concurrencyProp)
            ? concurrencyProp.GetInt32()
            : (int?)null;

        // Prepare source URIs
        var urisToProcess = new List<string>();

        if (sourceUris != null)
        {
            urisToProcess.AddRange(sourceUris);
        }
        else if (texts != null)
        {
            // Create temporary files for text ingestion
            var tempFiles = new List<string>();
            try
            {
                for (int i = 0; i < texts.Count; i++)
                {
                    var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.txt");
                    await File.WriteAllTextAsync(tempFile, texts[i], context.RequestAborted);
                    tempFiles.Add(tempFile);
                    urisToProcess.Add(tempFile);
                }

                // Process batch ingestion
                var options = new IndexOptions(tenantId, null, null, null);
                var result = await pipeline.IngestBatchAsync(
                    urisToProcess,
                    options,
                    maxConcurrency: maxConcurrency,
                    cancellationToken: context.RequestAborted);

                var output = new AiOutputEnvelope(
                    result.Errors.Count > 0 ? OutputStatus.Partial : OutputStatus.Success,
                    "ingest.batch",
                    JsonSerializer.SerializeToElement(result),
                    null,
                    null,
                    result.Errors.Select(e => e.ErrorMessage).ToList());

                return Results.Ok(output);
            }
            finally
            {
                // Clean up temporary files
                foreach (var tempFile in tempFiles)
                {
                    if (File.Exists(tempFile))
                    {
                        try
                        {
                            File.Delete(tempFile);
                        }
                        catch
                        {
                            // Ignore cleanup errors
                        }
                    }
                }
            }
        }

        // Process batch ingestion with source URIs
        var indexOptions = new IndexOptions(tenantId, null, null, null);
        var batchResult = await pipeline.IngestBatchAsync(
            urisToProcess,
            indexOptions,
            maxConcurrency: maxConcurrency,
            cancellationToken: context.RequestAborted);

        var batchOutput = new AiOutputEnvelope(
            batchResult.Errors.Count > 0 ? OutputStatus.Partial : OutputStatus.Success,
            "ingest.batch",
            JsonSerializer.SerializeToElement(batchResult),
            null,
            null,
            batchResult.Errors.Select(e => e.ErrorMessage).ToList());

        return Results.Ok(batchOutput);
    }
    catch (Exception ex)
    {
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error in batch ingestion endpoint");
        return Results.Problem(
            detail: ex.Message,
            statusCode: 500);
    }
})
.RequireAuthorization()
.WithName("IngestBatch")
.WithOpenApi();

app.MapPost("/v1/chat/stream", async (
    HttpContext context,
    IModelRouter router,
    IRetriever retriever,
    IRagComposer composer,
    JsonDocument request) =>
{
    var tenantId = context.User.FindFirst("tenantId")?.Value ?? "default";
    var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();

    using var activity = BipinsAiActivitySource.Instance.StartActivity("api.chat.stream");

    try
    {
        // Parse input envelope
        var input = JsonSerializer.Deserialize<AiInputEnvelope>(request.RootElement.GetRawText());
        if (input == null)
        {
            return Results.BadRequest(new { error = "Invalid input envelope" });
        }

        tenantId = input.TenantId;

        // Parse chat request from payload
        var chatRequestJson = input.Payload;
        var messages = chatRequestJson.TryGetProperty("messages", out var messagesProp)
            ? JsonSerializer.Deserialize<List<Message>>(messagesProp.GetRawText())
            : null;

        if (messages == null || messages.Count == 0)
        {
            return Results.BadRequest(new { error = "messages are required" });
        }

        var chatRequest = new ChatRequest(messages);

        // Retrieve relevant chunks (RAG)
        var retrieveRequest = new RetrieveRequest(
            messages.Last().Content,
            TopK: 5);

        var retrieved = await retriever.RetrieveAsync(retrieveRequest, context.RequestAborted);

        // Compose augmented request
        var augmentedRequest = composer.Compose(chatRequest, retrieved);

        // Generate streaming response
        var chatModel = await router.SelectChatModelAsync(tenantId, augmentedRequest, context.RequestAborted);
        
        // Check if model supports streaming
        if (chatModel is not IChatModelStreaming streamingModel)
        {
            return Results.BadRequest(new { error = "Selected model does not support streaming" });
        }

        context.Response.ContentType = "text/event-stream";
        context.Response.Headers.Append("Cache-Control", "no-cache");
        context.Response.Headers.Append("Connection", "keep-alive");

        await foreach (var chunk in streamingModel.GenerateStreamAsync(augmentedRequest, context.RequestAborted))
        {
            var chunkJson = JsonSerializer.Serialize(chunk);
            await context.Response.WriteAsync($"data: {chunkJson}\n\n", context.RequestAborted);
            await context.Response.Body.FlushAsync(context.RequestAborted);
        }

        await context.Response.WriteAsync("data: [DONE]\n\n", context.RequestAborted);
        await context.Response.Body.FlushAsync(context.RequestAborted);

        return Results.Empty;
    }
    catch (Exception ex)
    {
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error in streaming chat endpoint");
        return Results.Problem(
            detail: ex.Message,
            statusCode: 500);
    }
})
.RequireAuthorization()
.WithName("ChatStream")
.WithOpenApi();

app.MapPost("/v1/chat", async (
    HttpContext context,
    IModelRouter router,
    IRetriever retriever,
    IRagComposer composer,
    JsonDocument request) =>
{
    var tenantId = context.User.FindFirst("tenantId")?.Value ?? "default";
    var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();

    using var activity = BipinsAiActivitySource.Instance.StartActivity("api.chat");

    try
    {
        // Parse input envelope
        var input = JsonSerializer.Deserialize<AiInputEnvelope>(request.RootElement.GetRawText());
        if (input == null)
        {
            return Results.BadRequest(new { error = "Invalid input envelope" });
        }

        tenantId = input.TenantId;

        // Parse chat request from payload
        var chatRequestJson = input.Payload;
        var messages = chatRequestJson.TryGetProperty("messages", out var messagesProp)
            ? JsonSerializer.Deserialize<List<Message>>(messagesProp.GetRawText())
            : null;

        if (messages == null || messages.Count == 0)
        {
            return Results.BadRequest(new { error = "messages are required" });
        }

        // Parse tools if provided
        var tools = chatRequestJson.TryGetProperty("tools", out var toolsProp)
            ? JsonSerializer.Deserialize<List<ToolDefinition>>(toolsProp.GetRawText())
            : null;

        // Parse tool choice if provided
        var toolChoice = chatRequestJson.TryGetProperty("toolChoice", out var toolChoiceProp)
            ? toolChoiceProp.GetString()
            : null;

        // Parse structured output if provided
        StructuredOutputOptions? structuredOutput = null;
        if (chatRequestJson.TryGetProperty("structuredOutput", out var structuredOutputProp))
        {
            var schema = structuredOutputProp.TryGetProperty("schema", out var schemaProp)
                ? schemaProp
                : default;
            var responseFormat = structuredOutputProp.TryGetProperty("responseFormat", out var formatProp)
                ? formatProp.GetString() ?? "json_schema"
                : "json_schema";
            
            if (schema.ValueKind != JsonValueKind.Undefined)
            {
                structuredOutput = new StructuredOutputOptions(schema, responseFormat);
            }
        }

        // Parse other optional parameters
        var temperature = chatRequestJson.TryGetProperty("temperature", out var tempProp)
            ? tempProp.GetSingle()
            : (float?)null;
        var maxTokens = chatRequestJson.TryGetProperty("maxTokens", out var maxTokensProp)
            ? maxTokensProp.GetInt32()
            : (int?)null;

        var chatRequest = new ChatRequest(
            messages,
            tools,
            toolChoice,
            temperature,
            maxTokens,
            null,
            structuredOutput);

        // Retrieve relevant chunks (RAG)
        var retrieveRequest = new RetrieveRequest(
            messages.Last().Content,
            TopK: 5);

        var retrieved = await retriever.RetrieveAsync(retrieveRequest, context.RequestAborted);

        // Compose augmented request
        var augmentedRequest = composer.Compose(chatRequest, retrieved);

        // Generate response
        var chatModel = await router.SelectChatModelAsync(tenantId, augmentedRequest, context.RequestAborted);
        var response = await chatModel.GenerateAsync(augmentedRequest, context.RequestAborted);

        // Parse structured output if requested
        JsonElement? parsedStructuredOutput = null;
        if (chatRequest.StructuredOutput != null && !string.IsNullOrEmpty(response.Content))
        {
            parsedStructuredOutput = StructuredOutputHelper.ExtractStructuredOutput(response.Content);
            if (parsedStructuredOutput.HasValue)
            {
                var validated = StructuredOutputHelper.ParseAndValidate(
                    response.Content,
                    chatRequest.StructuredOutput.Schema);
                if (validated.HasValue)
                {
                    parsedStructuredOutput = validated;
                }
            }
        }

        // Update response with structured output if parsed
        var finalResponse = parsedStructuredOutput.HasValue
            ? response with { StructuredOutput = parsedStructuredOutput }
            : response;

        // Build citations
        var citations = retrieved.Chunks.Select(c => new Citation(
            c.SourceUri,
            c.DocId,
            c.Chunk.Id,
            c.Chunk.Text,
            c.Score)).ToList();

        var telemetry = response.Usage != null
            ? new ModelTelemetry(
                response.ModelId ?? "unknown",
                response.Usage.TotalTokens,
                0, // TODO: Calculate latency
                "OpenAI")
            : null;

        var output = new AiOutputEnvelope(
            OutputStatus.Success,
            "chat",
            JsonSerializer.SerializeToElement(finalResponse),
            citations,
            telemetry);

        return Results.Ok(output);
    }
    catch (Exception ex)
    {
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        return Results.Problem(ex.Message);
    }
})
.RequireAuthorization()
.WithName("Chat")
.WithOpenApi();

app.MapPost("/v1/query", async (
    HttpContext context,
    IRetriever retriever,
    JsonDocument request) =>
{
    var tenantId = context.User.FindFirst("tenantId")?.Value ?? "default";

    var query = request.RootElement.TryGetProperty("query", out var queryProp)
        ? queryProp.GetString()
        : null;

    if (string.IsNullOrEmpty(query))
    {
        return Results.BadRequest(new { error = "query is required" });
    }

    var topK = request.RootElement.TryGetProperty("topK", out var topKProp)
        ? topKProp.GetInt32()
        : 5;

    var retrieveRequest = new RetrieveRequest(query, TopK: topK);
    var result = await retriever.RetrieveAsync(retrieveRequest, context.RequestAborted);

    var output = new AiOutputEnvelope(
        OutputStatus.Success,
        "query",
        JsonSerializer.SerializeToElement(result),
        result.Chunks.Select(c => new Citation(
            c.SourceUri,
            c.DocId,
            c.Chunk.Id,
            c.Chunk.Text,
            c.Score)).ToList());

    return Results.Ok(output);
})
.RequireAuthorization()
.WithName("Query")
.WithOpenApi();

app.MapGet("/v1/documents/{docId}/versions", async (
    HttpContext context,
    string docId,
    IDocumentVersionManager versionManager) =>
{
    var tenantId = context.User.FindFirst("tenantId")?.Value ?? "default";

    using var activity = BipinsAiActivitySource.Instance.StartActivity("api.documents.versions.list");

    try
    {
        var versions = await versionManager.ListVersionsAsync(
            tenantId,
            docId,
            cancellationToken: context.RequestAborted);

        var output = new AiOutputEnvelope(
            OutputStatus.Success,
            "document.versions",
            JsonSerializer.SerializeToElement(versions));

        return Results.Ok(output);
    }
    catch (Exception ex)
    {
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error listing versions for document {DocId}", docId);
        return Results.Problem(
            detail: ex.Message,
            statusCode: 500);
    }
})
.RequireAuthorization()
.WithName("ListDocumentVersions")
.WithOpenApi();

app.MapGet("/v1/documents/{docId}/versions/{versionId}", async (
    HttpContext context,
    string docId,
    string versionId,
    IDocumentVersionManager versionManager) =>
{
    var tenantId = context.User.FindFirst("tenantId")?.Value ?? "default";

    using var activity = BipinsAiActivitySource.Instance.StartActivity("api.documents.versions.get");

    try
    {
        var version = await versionManager.GetVersionAsync(
            tenantId,
            docId,
            versionId,
            cancellationToken: context.RequestAborted);

        if (version == null)
        {
            return Results.NotFound(new { error = $"Version {versionId} not found for document {docId}" });
        }

        var output = new AiOutputEnvelope(
            OutputStatus.Success,
            "document.version",
            JsonSerializer.SerializeToElement(version));

        return Results.Ok(output);
    }
    catch (Exception ex)
    {
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error getting version {VersionId} for document {DocId}", versionId, docId);
        return Results.Problem(
            detail: ex.Message,
            statusCode: 500);
    }
})
.RequireAuthorization()
.WithName("GetDocumentVersion")
.WithOpenApi();

app.Run();
