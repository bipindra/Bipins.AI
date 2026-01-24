using System.Diagnostics;
using Bipins.AI.Core.Models;
using Bipins.AI.Core.Vector;

namespace Bipins.AI.Runtime.Observability;

/// <summary>
/// Extension methods for OpenTelemetry activities.
/// </summary>
public static class ActivityExtensions
{
    /// <summary>
    /// Starts an activity for a chat model call.
    /// </summary>
    public static Activity? StartChatModelActivity(this ActivitySource source, ChatRequest request, string modelId)
    {
        var activity = source.StartActivity("ai.chat.generate");
        if (activity != null)
        {
            activity.SetTag("ai.model.id", modelId);
            activity.SetTag("ai.request.messages.count", request.Messages.Count);
            activity.SetTag("ai.request.temperature", request.Temperature);
            activity.SetTag("ai.request.max_tokens", request.MaxTokens);
        }

        return activity;
    }

    /// <summary>
    /// Starts an activity for an embedding model call.
    /// </summary>
    public static Activity? StartEmbeddingActivity(this ActivitySource source, EmbeddingRequest request, string modelId)
    {
        var activity = source.StartActivity("ai.embedding.generate");
        if (activity != null)
        {
            activity.SetTag("ai.model.id", modelId);
            activity.SetTag("ai.request.inputs.count", request.Inputs.Count);
        }

        return activity;
    }

    /// <summary>
    /// Starts an activity for a vector query.
    /// </summary>
    public static Activity? StartVectorQueryActivity(this ActivitySource source, VectorQueryRequest request)
    {
        var activity = source.StartActivity("vector.query");
        if (activity != null)
        {
            activity.SetTag("vector.query.top_k", request.TopK);
            activity.SetTag("vector.query.has_filter", request.Filter != null);
            activity.SetTag("vector.collection", request.CollectionName ?? "default");
        }

        return activity;
    }

    /// <summary>
    /// Starts an activity for a pipeline step.
    /// </summary>
    public static Activity? StartPipelineStepActivity(this ActivitySource source, string stepName)
    {
        var activity = source.StartActivity("pipeline.step");
        if (activity != null)
        {
            activity.SetTag("pipeline.step.name", stepName);
        }

        return activity;
    }

    /// <summary>
    /// Records usage information on an activity.
    /// </summary>
    public static void RecordUsage(this Activity activity, Usage usage)
    {
        activity.SetTag("ai.usage.prompt_tokens", usage.PromptTokens);
        activity.SetTag("ai.usage.completion_tokens", usage.CompletionTokens);
        activity.SetTag("ai.usage.total_tokens", usage.TotalTokens);
    }
}
