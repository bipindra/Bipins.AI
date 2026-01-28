using Bipins.AI.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bipins.AI.Providers.Bedrock;

/// <summary>
/// AWS Bedrock implementation of ILLMProvider.
/// </summary>
public class BedrockLLMProvider : ILLMProvider
{
    private readonly IChatModel _chatModel;
    private readonly IChatModelStreaming _chatModelStreaming;
    private readonly BedrockOptions _options;
    private readonly ILogger<BedrockLLMProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BedrockLLMProvider"/> class.
    /// </summary>
    public BedrockLLMProvider(
        IChatModel chatModel,
        IChatModelStreaming chatModelStreaming,
        IOptions<BedrockOptions> options,
        ILogger<BedrockLLMProvider> logger)
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
        throw new NotSupportedException("AWS Bedrock embedding support is not currently implemented. Please use OpenAI or Azure OpenAI for embeddings.");
    }
}
