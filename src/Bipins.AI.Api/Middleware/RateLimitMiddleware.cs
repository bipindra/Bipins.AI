using System.Net;
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
    private readonly Dictionary<string, RateLimitingPolicy> _tenantRateLimiters = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitMiddleware"/> class.
    /// </summary>
    public RateLimitMiddleware(
        RequestDelegate next,
        ILogger<RateLimitMiddleware> logger,
        ILoggerFactory loggerFactory,
        IOptions<RateLimitingOptions> options)
    {
        _next = next;
        _logger = logger;
        _loggerFactory = loggerFactory;
        _options = options.Value;
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
        var rateLimiter = GetOrCreateRateLimiter(tenantId);

        try
        {
            await rateLimiter.ExecuteAsync(async ct =>
            {
                await _next(context);
            }, context.RequestAborted);
        }
        catch (OperationCanceledException)
        {
            // Request was cancelled, don't set status code
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rate limiting error for tenant {TenantId}", tenantId);
            
            // If rate limit was exceeded, return 429
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.ContentType = "application/json";
            
            var retryAfter = rateLimiter.GetRetryAfterSeconds();
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
    }

    private RateLimitingPolicy GetOrCreateRateLimiter(string tenantId)
    {
        lock (_tenantRateLimiters)
        {
            if (!_tenantRateLimiters.TryGetValue(tenantId, out var limiter))
            {
                var policyLogger = _loggerFactory.CreateLogger<RateLimitingPolicy>();
                limiter = new RateLimitingPolicy(policyLogger, _options);
                _tenantRateLimiters[tenantId] = limiter;
            }
            return limiter;
        }
    }
}
