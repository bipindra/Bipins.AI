namespace Bipins.AI.Core.Models;

/// <summary>
/// Role of a message in a conversation.
/// </summary>
public enum MessageRole
{
    /// <summary>
    /// System message (instructions, context).
    /// </summary>
    System,

    /// <summary>
    /// User message.
    /// </summary>
    User,

    /// <summary>
    /// Assistant message.
    /// </summary>
    Assistant,

    /// <summary>
    /// Tool/function response.
    /// </summary>
    Tool
}
