using Bipins.AI.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bipins.AI.Providers.OpenAI;

/// <summary>
/// OpenAI implementation of ILLMProvider.
/// </summary>
public class OpenAiLLMProvider : ILLMProvider
{
    private readonly IChatModel _chatModel;
    private readonly IChatModelStreaming _chatModelStreaming;
    private readonly IEmbeddingModel _embeddingModel;
    private readonly OpenAiOptions _options;
    private readonly ILogger<OpenAiLLMProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAiLLMProvider"/> class.
    /// </summary>
    public OpenAiLLMProvider(
        IChatModel chatModel,
        IChatModelStreaming chatModelStreaming,
        IEmbeddingModel embeddingModel,
        IOptions<OpenAiOptions> options,
        ILogger<OpenAiLLMProvider> logger)
    {
        _chatModel = chatModel;
        _chatModelStreaming = chatModelStreaming;
        _embeddingModel = embeddingModel;
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
    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var request = new EmbeddingRequest(
            Inputs: new[] { text },
            ModelId: _options.DefaultEmbeddingModelId);

        var response = await _embeddingModel.EmbedAsync(request, cancellationToken);
        
        if (response.Vectors.Count == 0)
        {
            throw new InvalidOperationException("No embedding vector returned from OpenAI");
        }

        return response.Vectors[0].ToArray();
    }
}
