using System.Net;
using Bipins.AI.Core.Runtime.Policies;
using Bipins.AI.Runtime.Policies;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bipins.AI.Api.Middleware;

/// <summary>
/// Middleware for rate limiting API requests.
/// </summary>
public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitMiddleware> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly RateLimitingOptions _options;
    private readonly IRateLimiter _rateLimiter;
    private readonly Dictionary<string, RateLimitingPolicy> _tenantRateLimiters = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitMiddleware"/> class.
    /// </summary>
    public RateLimitMiddleware(
        RequestDelegate next,
        ILogger<RateLimitMiddleware> logger,
        ILoggerFactory loggerFactory,
        IOptions<RateLimitingOptions> options,
        IRateLimiter rateLimiter)
    {
        _next = next;
        _logger = logger;
        _loggerFactory = loggerFactory;
        _options = options.Value;
        _rateLimiter = rateLimiter;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        // Skip rate limiting for health checks and other system endpoints
        if (context.Request.Path.StartsWithSegments("/health") ||
            context.Request.Path.StartsWithSegments("/metrics"))
        {
            await _next(context);
            return;
        }

        var tenantId = context.User.FindFirst("tenantId")?.Value ?? "default";
        var endpoint = context.Request.Path.Value ?? "/";
        var rateLimitKey = $"tenant:{tenantId}:endpoint:{endpoint}";
        
        var rateLimiter = GetOrCreateRateLimiter(tenantId);

        try
        {
            await rateLimiter.ExecuteAsync(rateLimitKey, async ct =>
            {
                await _next(context);
            }, context.RequestAborted);
        }
        catch (OperationCanceledException)
        {
            // Request was cancelled, don't set status code
            throw;
        }
        catch (RateLimitExceededException)
        {
            // Rate limit exceeded, return 429
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.ContentType = "application/json";
            
            var retryAfter = await rateLimiter.GetRetryAfterSecondsAsync(rateLimitKey, context.RequestAborted);
            if (retryAfter.HasValue)
            {
                context.Response.Headers["Retry-After"] = retryAfter.Value.ToString();
            }

            await context.Response.WriteAsJsonAsync(new
            {
                error = "Rate limit exceeded",
                message = "Too many requests. Please try again later.",
                retryAfter = retryAfter
            }, context.RequestAborted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rate limiting error for tenant {TenantId}", tenantId);
            throw;
        }
    }

    private RateLimitingPolicy GetOrCreateRateLimiter(string tenantId)
    {
        lock (_tenantRateLimiters)
        {
            if (!_tenantRateLimiters.TryGetValue(tenantId, out var limiter))
            {
                var policyLogger = _loggerFactory.CreateLogger<RateLimitingPolicy>();
                limiter = new RateLimitingPolicy(policyLogger, _options, _rateLimiter);
                _tenantRateLimiters[tenantId] = limiter;
            }
            return limiter;
        }
    }
}
