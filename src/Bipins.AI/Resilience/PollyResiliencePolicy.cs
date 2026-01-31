using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;
using Polly.Bulkhead;

namespace Bipins.AI.Resilience;

/// <summary>
/// Polly-based implementation of resilience policy.
/// </summary>
public class PollyResiliencePolicy : IResiliencePolicy
{
    private readonly IAsyncPolicy _policy;
    private readonly ILogger<PollyResiliencePolicy>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PollyResiliencePolicy"/> class.
    /// </summary>
    public PollyResiliencePolicy(ResilienceOptions options, ILogger<PollyResiliencePolicy>? logger = null)
    {
        _logger = logger;
        _policy = BuildPolicy(options);
    }

    /// <summary>
    /// Initializes a new instance with a pre-built Polly policy.
    /// </summary>
    public PollyResiliencePolicy(IAsyncPolicy policy, ILogger<PollyResiliencePolicy>? logger = null)
    {
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        await _policy.ExecuteAsync(async ct => await action(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        return await _policy.ExecuteAsync(async ct => await action(), cancellationToken);
    }

    private IAsyncPolicy BuildPolicy(ResilienceOptions options)
    {
        IAsyncPolicy policy = Policy.NoOpAsync();

        // Build retry policy
        if (options.Retry != null)
        {
            var retryPolicy = BuildRetryPolicy(options.Retry);
            policy = policy.WrapAsync(retryPolicy);
        }

        // Build circuit breaker policy
        // Note: Circuit breaker requires Polly.CircuitBreaker package
        // For now, circuit breaker is disabled until proper package is added
        // if (options.CircuitBreaker != null)
        // {
        //     var circuitBreakerPolicy = BuildCircuitBreakerPolicy(options.CircuitBreaker);
        //     policy = policy.WrapAsync(circuitBreakerPolicy);
        // }

        // Build timeout policy
        if (options.Timeout != null)
        {
            var timeoutPolicy = Policy.TimeoutAsync(options.Timeout.Timeout);
            policy = policy.WrapAsync(timeoutPolicy);
        }

        // Build bulkhead policy
        if (options.Bulkhead != null)
        {
            var bulkheadPolicy = Policy.BulkheadAsync(
                maxParallelization: options.Bulkhead.MaxParallelization,
                maxQueuingActions: options.Bulkhead.MaxQueuingActions);
            policy = policy.WrapAsync(bulkheadPolicy);
        }

        return policy;
    }

    private IAsyncPolicy BuildRetryPolicy(RetryOptions options)
    {
        var retryPolicyBuilder = Policy
            .Handle<Exception>(ex => 
            {
                if (options.RetryableExceptions != null && options.RetryableExceptions.Count > 0)
                {
                    return options.RetryableExceptions.Any(t => t.IsInstanceOfType(ex));
                }
                return true; // Retry on all exceptions by default
            })
            .WaitAndRetryAsync(
                retryCount: options.MaxRetries,
                sleepDurationProvider: (retryAttempt) => CalculateDelay(retryAttempt, options),
                onRetry: (exception, timespan, retryCount, context) =>
                {
                    _logger?.LogWarning(
                        "Retry {RetryCount} after {Delay}ms. Exception: {Exception}",
                        retryCount,
                        timespan.TotalMilliseconds,
                        exception?.Message);
                });

        return retryPolicyBuilder;
    }

    // Circuit breaker implementation - requires Polly.CircuitBreaker package
    // Uncomment and add package reference when needed
    /*
    private IAsyncPolicy BuildCircuitBreakerPolicy(CircuitBreakerOptions options)
    {
        // Create circuit breaker policy using Polly v8 API
        var circuitBreakerPolicy = Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: options.FailureThreshold,
                durationOfBreak: options.DurationOfBreak,
                onBreak: (exception, duration) =>
                {
                    _logger?.LogError(
                        "Circuit breaker opened for {Duration}ms. Exception: {Exception}",
                        duration.TotalMilliseconds,
                        exception?.Message ?? "Unknown");
                },
                onReset: () =>
                {
                    _logger?.LogInformation("Circuit breaker reset");
                });

        return circuitBreakerPolicy;
    }
    */

    private static TimeSpan CalculateDelay(int retryAttempt, RetryOptions options)
    {
        var delay = options.BackoffStrategy switch
        {
            BackoffStrategy.Fixed => options.Delay,
            BackoffStrategy.Linear => TimeSpan.FromMilliseconds(options.Delay.TotalMilliseconds * retryAttempt),
            BackoffStrategy.Exponential => TimeSpan.FromMilliseconds(
                options.Delay.TotalMilliseconds * Math.Pow(2, retryAttempt - 1)),
            _ => options.Delay
        };

        if (options.MaxDelay.HasValue && delay > options.MaxDelay.Value)
        {
            delay = options.MaxDelay.Value;
        }

        return delay;
    }
}

/// <summary>
/// Factory for creating resilience policies from options.
/// </summary>
public class ResiliencePolicyFactory : IResiliencePolicyFactory
{
    private readonly ILogger<PollyResiliencePolicy>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResiliencePolicyFactory"/> class.
    /// </summary>
    public ResiliencePolicyFactory(ILogger<PollyResiliencePolicy>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public IResiliencePolicy CreatePolicy(ResilienceOptions options)
    {
        return new PollyResiliencePolicy(options, _logger);
    }
}
