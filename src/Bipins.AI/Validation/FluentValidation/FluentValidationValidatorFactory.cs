using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Bipins.AI.Validation.FluentValidation;

/// <summary>
/// Factory for creating FluentValidation validators.
/// </summary>
public class FluentValidationValidatorFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FluentValidationValidatorFactory"/> class.
    /// </summary>
    public FluentValidationValidatorFactory(IServiceProvider serviceProvider, ILogger? logger = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Creates a validator for the specified type.
    /// </summary>
    public IRequestValidator<T>? CreateValidator<T>()
    {
        var validator = _serviceProvider.GetService<IValidator<T>>();
        if (validator == null)
        {
            _logger?.LogDebug("No FluentValidation validator found for type {Type}", typeof(T).Name);
            return null;
        }

        return new FluentValidationValidator<T>(validator, _logger as ILogger<FluentValidationValidator<T>>);
    }
}
