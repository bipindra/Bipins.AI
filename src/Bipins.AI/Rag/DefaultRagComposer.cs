using Bipins.AI.Core.Models;
using Bipins.AI.Core.Rag;
using Bipins.AI.SemanticKernel;
using Microsoft.Extensions.Logging;

namespace Bipins.AI.Runtime.Rag;

/// <summary>
/// Composes RAG-augmented chat requests with retrieved context.
/// </summary>
public class DefaultRagComposer : IRagComposer
{
    private readonly ILogger<DefaultRagComposer> _logger;
    private readonly ISemanticKernelBridge _semanticKernelBridge;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultRagComposer"/> class.
    /// </summary>
    public DefaultRagComposer(
        ILogger<DefaultRagComposer> logger,
        ISemanticKernelBridge semanticKernelBridge)
    {
        _logger = logger;
        _semanticKernelBridge = semanticKernelBridge;
    }

    /// <inheritdoc />
    public ChatRequest Compose(ChatRequest original, RetrieveResult retrieved)
    {
        if (retrieved.Chunks.Count == 0)
        {
            _logger.LogWarning("No chunks retrieved for RAG composition");
            return original;
        }

        // Create augmented messages
        var messages = new List<Message>();

        // Add system message with context if not already present
        var hasSystemMessage = original.Messages.Any(m => m.Role == MessageRole.System);
        var existingSystemPrompt = hasSystemMessage
            ? original.Messages.First(m => m.Role == MessageRole.System).Content
            : null;
        var ragPrompt = _semanticKernelBridge.RenderRagSystemPrompt(new RagPromptTemplate(
            retrieved.Chunks.Select(c => new RagPromptSource(c.Chunk.Text, c.SourceUri, c.DocId)).ToList(),
            existingSystemPrompt));
        messages.Add(new Message(MessageRole.System, ragPrompt));

        // Add remaining messages
        messages.AddRange(original.Messages.Where(m => m.Role != MessageRole.System));

        _logger.LogInformation(
            "Composed RAG request with {ChunkCount} chunks, {MessageCount} messages",
            retrieved.Chunks.Count,
            messages.Count);

        return original with { Messages = messages };
    }
}
