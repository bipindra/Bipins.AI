using Bipins.AI.Core.Models;

namespace Bipins.AI.Core.Rag;

/// <summary>
/// Contract for composing RAG-augmented chat requests.
/// </summary>
public interface IRagComposer
{
    /// <summary>
    /// Composes a chat request augmented with retrieved context.
    /// </summary>
    /// <param name="original">The original chat request.</param>
    /// <param name="retrieved">The retrieved chunks.</param>
    /// <returns>An augmented chat request with context.</returns>
    ChatRequest Compose(ChatRequest original, RetrieveResult retrieved);
}
