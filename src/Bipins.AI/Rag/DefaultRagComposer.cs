using Bipins.AI.Core.Models;
using Bipins.AI.Core.Rag;
using Microsoft.Extensions.Logging;

namespace Bipins.AI.Runtime.Rag;

/// <summary>
/// Composes RAG-augmented chat requests with retrieved context.
/// </summary>
public class DefaultRagComposer : IRagComposer
{
    private readonly ILogger<DefaultRagComposer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultRagComposer"/> class.
    /// </summary>
    public DefaultRagComposer(ILogger<DefaultRagComposer> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public ChatRequest Compose(ChatRequest original, RetrieveResult retrieved)
    {
        if (retrieved.Chunks.Count == 0)
        {
            _logger.LogWarning("No chunks retrieved for RAG composition");
            return original;
        }

        // Build context from retrieved chunks
        var contextBuilder = new System.Text.StringBuilder();
        contextBuilder.AppendLine("Use the following context to answer the question. Cite sources when possible.");
        contextBuilder.AppendLine();

        for (int i = 0; i < retrieved.Chunks.Count; i++)
        {
            var chunk = retrieved.Chunks[i];
            contextBuilder.AppendLine($"[Source {i + 1}]");
            if (!string.IsNullOrEmpty(chunk.SourceUri))
            {
                contextBuilder.AppendLine($"Source: {chunk.SourceUri}");
            }

            if (!string.IsNullOrEmpty(chunk.DocId))
            {
                contextBuilder.AppendLine($"Document: {chunk.DocId}");
            }

            contextBuilder.AppendLine($"Content: {chunk.Chunk.Text}");
            contextBuilder.AppendLine();
        }

        var context = contextBuilder.ToString();

        // Create augmented messages
        var messages = new List<Message>();

        // Add system message with context if not already present
        var hasSystemMessage = original.Messages.Any(m => m.Role == MessageRole.System);
        if (!hasSystemMessage)
        {
            messages.Add(new Message(MessageRole.System, context));
        }
        else
        {
            // Prepend context to existing system message
            var systemMsg = original.Messages.First(m => m.Role == MessageRole.System);
            messages.Add(new Message(MessageRole.System, $"{context}\n\n{systemMsg.Content}"));
        }

        // Add remaining messages
        messages.AddRange(original.Messages.Where(m => m.Role != MessageRole.System));

        _logger.LogInformation(
            "Composed RAG request with {ChunkCount} chunks, {MessageCount} messages",
            retrieved.Chunks.Count,
            messages.Count);

        return original with { Messages = messages };
    }
}
