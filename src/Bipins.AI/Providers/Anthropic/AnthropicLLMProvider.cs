using Bipins.AI.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bipins.AI.Providers.Anthropic;

/// <summary>
/// Anthropic implementation of ILLMProvider.
/// </summary>
public class AnthropicLLMProvider : ILLMProvider
{
    private readonly IChatModel _chatModel;
    private readonly IChatModelStreaming _chatModelStreaming;
    private readonly AnthropicOptions _options;
    private readonly ILogger<AnthropicLLMProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnthropicLLMProvider"/> class.
    /// </summary>
    public AnthropicLLMProvider(
        IChatModel chatModel,
        IChatModelStreaming chatModelStreaming,
        IOptions<AnthropicOptions> options,
        ILogger<AnthropicLLMProvider> logger)
    {
        _chatModel = chatModel;
        _chatModelStreaming = chatModelStreaming;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public IChatModel CurrentModel => _chatModel;

    /// <inheritdoc />
    public Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        return _chatModel.GenerateAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<ChatResponseChunk> ChatStreamAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        return _chatModelStreaming.GenerateStreamAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Anthropic does not currently support embedding generation. Please use OpenAI or Azure OpenAI for embeddings.");
    }
}
