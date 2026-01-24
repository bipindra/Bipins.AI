namespace Bipins.AI.Core.Models;

/// <summary>
/// Represents a message in a chat conversation.
/// </summary>
/// <param name="Role">The role of the message sender.</param>
/// <param name="Content">The message content.</param>
/// <param name="ToolCallId">Optional tool call ID for function responses.</param>
public record Message(
    MessageRole Role,
    string Content,
    string? ToolCallId = null);
