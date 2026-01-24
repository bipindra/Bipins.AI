namespace Bipins.AI.Core.Models;

/// <summary>
/// Token usage information.
/// </summary>
/// <param name="PromptTokens">Number of tokens in the prompt.</param>
/// <param name="CompletionTokens">Number of tokens in the completion.</param>
/// <param name="TotalTokens">Total tokens used.</param>
public record Usage(
    int PromptTokens,
    int CompletionTokens,
    int TotalTokens);
