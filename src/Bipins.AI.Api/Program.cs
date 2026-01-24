using System.Diagnostics;
using System.Text.Json;
using Bipins.AI.Api.Authentication;
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
using Bipins.AI.Runtime.Observability;
using Bipins.AI.Runtime.Rag;
using Bipins.AI.Runtime.Routing;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Bipins.AI services
builder.Services.AddBipinsAI();
builder.Services.AddBipinsAIRuntime();
builder.Services.AddBipinsAIIngestion();
builder.Services.AddBipinsAIRag();
builder.Services
    .AddBipinsAI()
    .AddOpenAI(o =>
    {
        o.ApiKey = builder.Configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI:ApiKey not configured");
        o.BaseUrl = builder.Configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1";
        o.DefaultChatModelId = builder.Configuration["OpenAI:DefaultChatModelId"] ?? "gpt-3.5-turbo";
        o.DefaultEmbeddingModelId = builder.Configuration["OpenAI:DefaultEmbeddingModelId"] ?? "text-embedding-ada-002";
    })
    .AddQdrant(o =>
    {
        o.Endpoint = builder.Configuration["Qdrant:Endpoint"] ?? "http://localhost:6333";
        o.ApiKey = builder.Configuration["Qdrant:ApiKey"];
        o.DefaultCollectionName = builder.Configuration["Qdrant:CollectionName"] ?? "default";
        o.VectorSize = int.Parse(builder.Configuration["Qdrant:VectorSize"] ?? "1536");
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
        var options = new IndexOptions(tenantId, docId, null, null);
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

        var chatRequest = new ChatRequest(messages);

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
            JsonSerializer.SerializeToElement(response),
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

app.Run();
