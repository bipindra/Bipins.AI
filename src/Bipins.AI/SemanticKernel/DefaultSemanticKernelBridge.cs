using System.Text;
using Bipins.AI.Core.Models;

namespace Bipins.AI.SemanticKernel;

/// <summary>
/// Default bridge implementation that keeps current behavior while providing
/// a single seam for Semantic Kernel-based prompt/tool processing.
/// </summary>
internal sealed class DefaultSemanticKernelBridge : ISemanticKernelBridge
{
    public IReadOnlyList<ToolDefinition> MapTools(IReadOnlyList<ToolDefinition> tools)
    {
        // Adapter seam for future direct SK function metadata mapping.
        return tools;
    }

    public string RenderPlanningPrompt(PlanningPromptTemplate template)
    {
        var prompt = new StringBuilder();
        prompt.Append("Goal: ").AppendLine(template.Goal).AppendLine();

        if (!string.IsNullOrWhiteSpace(template.RequestContext))
        {
            prompt.Append("Context: ").AppendLine(template.RequestContext).AppendLine();
        }

        if (template.ConversationHistory.Count > 0)
        {
            prompt.AppendLine("Previous conversation:");
            foreach (var message in template.ConversationHistory.TakeLast(10))
            {
                prompt.Append(message.Role).Append(": ").AppendLine(message.Content);
            }
            prompt.AppendLine();
        }

        if (template.AvailableTools.Count > 0)
        {
            prompt.AppendLine("Available tools:");
            foreach (var tool in template.AvailableTools)
            {
                prompt.Append("- ").Append(tool.Name).Append(": ").AppendLine(tool.Description);
            }
            prompt.AppendLine();
        }

        prompt.Append("Create a detailed step-by-step plan to accomplish the goal. ");
        prompt.Append("Include which tools to use and what parameters they need.");
        return prompt.ToString();
    }

    public string RenderRagSystemPrompt(RagPromptTemplate template)
    {
        var contextBuilder = new StringBuilder();
        contextBuilder.AppendLine("Use the following context to answer the question. Cite sources when possible.");
        contextBuilder.AppendLine();

        for (int i = 0; i < template.Sources.Count; i++)
        {
            var source = template.Sources[i];
            contextBuilder.AppendLine($"[Source {i + 1}]");
            if (!string.IsNullOrEmpty(source.SourceUri))
            {
                contextBuilder.AppendLine($"Source: {source.SourceUri}");
            }

            if (!string.IsNullOrEmpty(source.DocumentId))
            {
                contextBuilder.AppendLine($"Document: {source.DocumentId}");
            }

            contextBuilder.AppendLine($"Content: {source.Content}");
            contextBuilder.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(template.ExistingSystemPrompt))
        {
            contextBuilder.AppendLine(template.ExistingSystemPrompt);
        }

        return contextBuilder.ToString();
    }
}
