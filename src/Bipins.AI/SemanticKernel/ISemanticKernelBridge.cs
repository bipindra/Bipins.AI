using Bipins.AI.Core.Models;

namespace Bipins.AI.SemanticKernel;

/// <summary>
/// Internal adapter boundary for Semantic Kernel-assisted operations.
/// </summary>
public interface ISemanticKernelBridge
{
    IReadOnlyList<ToolDefinition> MapTools(IReadOnlyList<ToolDefinition> tools);
    string RenderPlanningPrompt(PlanningPromptTemplate template);
    string RenderRagSystemPrompt(RagPromptTemplate template);
}

public record PlanningPromptTemplate(
    string Goal,
    string? RequestContext,
    IReadOnlyList<Message> ConversationHistory,
    IReadOnlyList<ToolDefinition> AvailableTools);

public record RagPromptTemplate(
    IReadOnlyList<RagPromptSource> Sources,
    string? ExistingSystemPrompt);

public record RagPromptSource(
    string Content,
    string? SourceUri,
    string? DocumentId);
