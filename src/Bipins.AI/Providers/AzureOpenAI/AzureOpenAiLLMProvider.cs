using Bipins.AI.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bipins.AI.Providers.AzureOpenAI;

/// <summary>
/// Azure OpenAI implementation of ILLMProvider.
/// </summary>
public class AzureOpenAiLLMProvider : ILLMProvider
{
    private readonly IChatModel _chatModel;
    private readonly IChatModelStreaming _chatModelStreaming;
    private readonly IEmbeddingModel _embeddingModel;
    private readonly AzureOpenAiOptions _options;
    private readonly ILogger<AzureOpenAiLLMProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureOpenAiLLMProvider"/> class.
    /// </summary>
    public AzureOpenAiLLMProvider(
        IChatModel chatModel,
        IChatModelStreaming chatModelStreaming,
        IEmbeddingModel embeddingModel,
        IOptions<AzureOpenAiOptions> options,
        ILogger<AzureOpenAiLLMProvider> logger)
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
            ModelId: _options.DefaultEmbeddingDeploymentName);

        var response = await _embeddingModel.EmbedAsync(request, cancellationToken);
        
        if (response.Vectors.Count == 0)
        {
            throw new InvalidOperationException("No embedding vector returned from Azure OpenAI");
        }

        return response.Vectors[0].ToArray();
    }
}
