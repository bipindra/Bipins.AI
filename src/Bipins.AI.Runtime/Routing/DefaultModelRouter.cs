using Bipins.AI.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bipins.AI.Runtime.Routing;

/// <summary>
/// Default rule-based model router.
/// </summary>
public class DefaultModelRouter : IModelRouter
{
    private readonly ILogger<DefaultModelRouter> _logger;
    private readonly IServiceProvider _serviceProvider;
    private List<IChatModel>? _chatModels;
    private List<IEmbeddingModel>? _embeddingModels;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultModelRouter"/> class.
    /// </summary>
    public DefaultModelRouter(
        ILogger<DefaultModelRouter> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    private List<IChatModel> GetChatModels()
    {
        if (_chatModels == null)
        {
            _chatModels = _serviceProvider.GetServices<IChatModel>().ToList();
            if (_chatModels.Count == 0)
            {
                _logger.LogWarning("No chat models registered in DI");
            }
        }

        return _chatModels;
    }

    private List<IEmbeddingModel> GetEmbeddingModels()
    {
        if (_embeddingModels == null)
        {
            _embeddingModels = _serviceProvider.GetServices<IEmbeddingModel>().ToList();
            if (_embeddingModels.Count == 0)
            {
                _logger.LogWarning("No embedding models registered in DI");
            }
        }

        return _embeddingModels;
    }

    /// <inheritdoc />
    public Task<IChatModel> SelectChatModelAsync(string tenantId, ChatRequest request, CancellationToken cancellationToken = default)
    {
        var models = GetChatModels();
        if (models.Count == 0)
        {
            throw new InvalidOperationException("No chat models registered");
        }

        // Simple rule: use first available model (can be enhanced with routing rules)
        var model = models[0];
        _logger.LogDebug("Selected chat model: {Type} for tenant {TenantId}", model.GetType().Name, tenantId);
        return Task.FromResult(model);
    }

    /// <inheritdoc />
    public Task<IEmbeddingModel> SelectEmbeddingModelAsync(string tenantId, EmbeddingRequest request, CancellationToken cancellationToken = default)
    {
        var models = GetEmbeddingModels();
        if (models.Count == 0)
        {
            throw new InvalidOperationException("No embedding models registered");
        }

        // Simple rule: use first available model (can be enhanced with routing rules)
        var model = models[0];
        _logger.LogDebug("Selected embedding model: {Type} for tenant {TenantId}", model.GetType().Name, tenantId);
        return Task.FromResult(model);
    }
}
