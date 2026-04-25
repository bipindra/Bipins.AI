using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Bipins.AI.Core.CostTracking;
using Bipins.AI.Core.Ingestion;
using Bipins.AI.Core.Models;
using Bipins.AI.Core.Rag;
using Bipins.AI.Runtime.Policies;
using Bipins.AI.Runtime.Rag;
using Bipins.AI.Runtime.Routing;

namespace Bipins.AI.Orchestration;

internal sealed class DefaultChatOrchestrationService : IChatOrchestrationService
{
    private readonly IModelRouter _router;
    private readonly IRetriever _retriever;
    private readonly IRagComposer _composer;
    private readonly ITenantQuotaEnforcer _quotaEnforcer;
    private readonly ICostCalculator _costCalculator;
    private readonly ICostTracker _costTracker;

    public DefaultChatOrchestrationService(
        IModelRouter router,
        IRetriever retriever,
        IRagComposer composer,
        ITenantQuotaEnforcer quotaEnforcer,
        ICostCalculator costCalculator,
        ICostTracker costTracker)
    {
        _router = router;
        _retriever = retriever;
        _composer = composer;
        _quotaEnforcer = quotaEnforcer;
        _costCalculator = costCalculator;
        _costTracker = costTracker;
    }

    public async Task<ChatOrchestrationResult> ExecuteAsync(
        string tenantId,
        ChatRequest chatRequest,
        CancellationToken cancellationToken = default)
    {
        var retrieved = await RetrieveAsync(tenantId, chatRequest, cancellationToken);
        var augmentedRequest = _composer.Compose(chatRequest, retrieved);
        var estimatedTokens = EstimateTokens(augmentedRequest);
        await EnsureQuotaAsync(tenantId, estimatedTokens, cancellationToken);

        var chatModel = await _router.SelectChatModelAsync(tenantId, augmentedRequest, cancellationToken);
        var stopwatch = Stopwatch.StartNew();
        var response = await chatModel.GenerateAsync(augmentedRequest, cancellationToken);
        stopwatch.Stop();

        await RecordUsageAndCostAsync(tenantId, estimatedTokens, response.Usage, response.ModelId, cancellationToken);

        JsonElement? parsed = null;
        if (chatRequest.StructuredOutput != null && !string.IsNullOrEmpty(response.Content))
        {
            parsed = StructuredOutputHelper.ExtractStructuredOutput(response.Content);
            if (parsed.HasValue)
            {
                var validated = StructuredOutputHelper.ParseAndValidate(response.Content, chatRequest.StructuredOutput.Schema);
                if (validated.HasValue)
                {
                    parsed = validated;
                }
            }
        }

        var finalResponse = parsed.HasValue ? response with { StructuredOutput = parsed } : response;
        return new ChatOrchestrationResult(finalResponse, retrieved, parsed, stopwatch.ElapsedMilliseconds);
    }

    public async IAsyncEnumerable<ChatOrchestrationStreamEvent> ExecuteStreamAsync(
        string tenantId,
        ChatRequest chatRequest,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var retrieved = await RetrieveAsync(tenantId, chatRequest, cancellationToken);
        var augmentedRequest = _composer.Compose(chatRequest, retrieved);
        var estimatedTokens = EstimateTokens(augmentedRequest);
        await EnsureQuotaAsync(tenantId, estimatedTokens, cancellationToken);
        var chatModel = await _router.SelectChatModelAsync(tenantId, augmentedRequest, cancellationToken);
        if (chatModel is not IChatModelStreaming streamingModel)
        {
            throw new InvalidOperationException("Selected model does not support streaming");
        }

        yield return new ChatOrchestrationStreamEvent("start");

        var stopwatch = Stopwatch.StartNew();
        var accumulated = new StringBuilder();
        Usage? finalUsage = null;
        string? finalModelId = null;
        string? finalFinishReason = null;

        await foreach (var chunk in streamingModel.GenerateStreamAsync(augmentedRequest, cancellationToken))
        {
            if (!string.IsNullOrEmpty(chunk.Content))
            {
                accumulated.Append(chunk.Content);
            }

            yield return new ChatOrchestrationStreamEvent("content", chunk.Content, chunk.IsComplete);

            if (chunk.IsComplete)
            {
                finalUsage = chunk.Usage;
                finalModelId = chunk.ModelId;
                finalFinishReason = chunk.FinishReason;
            }
        }

        stopwatch.Stop();
        await RecordUsageAndCostAsync(tenantId, estimatedTokens, finalUsage, finalModelId, cancellationToken);

        JsonElement? parsed = null;
        if (chatRequest.StructuredOutput != null && accumulated.Length > 0)
        {
            parsed = StructuredOutputHelper.ExtractStructuredOutput(accumulated.ToString());
            if (parsed.HasValue)
            {
                var validated = StructuredOutputHelper.ParseAndValidate(accumulated.ToString(), chatRequest.StructuredOutput.Schema);
                if (validated.HasValue)
                {
                    parsed = validated;
                }
            }
        }

        yield return new ChatOrchestrationStreamEvent(
            EventType: "complete",
            Content: accumulated.ToString(),
            Usage: finalUsage,
            ModelId: finalModelId,
            FinishReason: finalFinishReason,
            Retrieved: retrieved,
            ParsedStructuredOutput: parsed,
            ElapsedMilliseconds: stopwatch.ElapsedMilliseconds);
        yield return new ChatOrchestrationStreamEvent("done");
    }

    private async Task<RetrieveResult> RetrieveAsync(string tenantId, ChatRequest request, CancellationToken cancellationToken)
    {
        var query = request.Messages.LastOrDefault()?.Content;
        if (string.IsNullOrWhiteSpace(query))
        {
            return new RetrieveResult(Array.Empty<RagChunk>(), ReadOnlyMemory<float>.Empty, 0);
        }

        var retrieveRequest = new RetrieveRequest(query, tenantId, TopK: 5);
        return await _retriever.RetrieveAsync(retrieveRequest, cancellationToken);
    }

    private async Task EnsureQuotaAsync(string tenantId, int estimatedTokens, CancellationToken cancellationToken)
    {
        var allowed = await _quotaEnforcer.CanMakeChatRequestAsync(tenantId, estimatedTokens, cancellationToken);
        if (!allowed)
        {
            throw new InvalidOperationException("Quota exceeded for tenant");
        }
    }

    private async Task RecordUsageAndCostAsync(
        string tenantId,
        int estimatedTokens,
        Usage? usage,
        string? modelId,
        CancellationToken cancellationToken)
    {
        var tokensUsed = usage?.TotalTokens ?? estimatedTokens;
        await _quotaEnforcer.RecordChatRequestAsync(tenantId, tokensUsed, cancellationToken);

        if (usage == null || string.IsNullOrEmpty(modelId))
        {
            return;
        }

        var provider = GetProviderFromModelId(modelId);
        var cost = _costCalculator.CalculateChatCost(provider, modelId, usage.PromptTokens, usage.CompletionTokens);
        var costRecord = new CostRecord(
            Id: Guid.NewGuid().ToString(),
            TenantId: tenantId,
            OperationType: CostOperationType.Chat,
            Provider: provider,
            ModelId: modelId,
            TokensUsed: usage.TotalTokens,
            PromptTokens: usage.PromptTokens,
            CompletionTokens: usage.CompletionTokens,
            Cost: cost);

        await _costTracker.RecordCostAsync(costRecord, cancellationToken);
    }

    private static int EstimateTokens(ChatRequest request)
        => request.Messages.Sum(m => m.Content?.Length ?? 0) / 4;

    private static string GetProviderFromModelId(string modelId)
    {
        if (modelId.StartsWith("gpt-", StringComparison.OrdinalIgnoreCase) ||
            modelId.StartsWith("text-", StringComparison.OrdinalIgnoreCase))
        {
            return "OpenAI";
        }

        if (modelId.Contains("claude", StringComparison.OrdinalIgnoreCase) ||
            modelId.StartsWith("anthropic", StringComparison.OrdinalIgnoreCase))
        {
            return "Anthropic";
        }

        if (modelId.Contains("azure", StringComparison.OrdinalIgnoreCase) ||
            modelId.Contains("gpt-35", StringComparison.OrdinalIgnoreCase) ||
            modelId.Contains("gpt-4", StringComparison.OrdinalIgnoreCase))
        {
            return "AzureOpenAI";
        }

        if (modelId.Contains("bedrock", StringComparison.OrdinalIgnoreCase) ||
            modelId.Contains("amazon", StringComparison.OrdinalIgnoreCase) ||
            modelId.StartsWith("anthropic.claude", StringComparison.OrdinalIgnoreCase))
        {
            return "Bedrock";
        }

        return "Unknown";
    }
}
