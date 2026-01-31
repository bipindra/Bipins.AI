using Bipins.AI.Core.Models;
using Bipins.AI.Providers;
using Microsoft.Extensions.Logging;

namespace Bipins.AI.Safety.Middleware;

/// <summary>
/// Decorator for ILLMProvider that applies content moderation middleware.
/// </summary>
public class ModeratedLLMProvider : ILLMProvider
{
    private readonly ILLMProvider _innerProvider;
    private readonly IReadOnlyList<ILLMProviderMiddleware> _middleware;
    private readonly ILogger<ModeratedLLMProvider>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModeratedLLMProvider"/> class.
    /// </summary>
    public ModeratedLLMProvider(
        ILLMProvider innerProvider,
        IEnumerable<ILLMProviderMiddleware> middleware,
        ILogger<ModeratedLLMProvider>? logger = null)
    {
        _innerProvider = innerProvider ?? throw new ArgumentNullException(nameof(innerProvider));
        _middleware = middleware?.ToList() ?? new List<ILLMProviderMiddleware>();
        _logger = logger;
    }

    /// <inheritdoc />
    public IChatModel CurrentModel => _innerProvider.CurrentModel;

    /// <inheritdoc />
    public async Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var processedRequest = request;

        // Apply request middleware
        foreach (var mw in _middleware)
        {
            processedRequest = await mw.OnRequestAsync(processedRequest, cancellationToken);
        }

        // Call inner provider
        var response = await _innerProvider.ChatAsync(processedRequest, cancellationToken);

        // Apply response middleware
        var processedResponse = response;
        foreach (var mw in _middleware)
        {
            processedResponse = await mw.OnResponseAsync(processedRequest, processedResponse, cancellationToken);
        }

        return processedResponse;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatResponseChunk> ChatStreamAsync(
        ChatRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var processedRequest = request;

        // Apply request middleware
        foreach (var mw in _middleware)
        {
            processedRequest = await mw.OnRequestAsync(processedRequest, cancellationToken);
        }

        // Stream from inner provider
        await foreach (var chunk in _innerProvider.ChatStreamAsync(processedRequest, cancellationToken))
        {
            yield return chunk;
        }
    }

    /// <inheritdoc />
    public async Task<float[]> GenerateEmbeddingAsync(string input, CancellationToken cancellationToken = default)
    {
        var processedInput = input;

        // Apply embedding request middleware
        foreach (var mw in _middleware)
        {
            processedInput = await mw.OnEmbeddingRequestAsync(processedInput, cancellationToken);
        }

        // Call inner provider
        return await _innerProvider.GenerateEmbeddingAsync(processedInput, cancellationToken);
    }
}
