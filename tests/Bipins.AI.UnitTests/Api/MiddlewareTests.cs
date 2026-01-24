using Bipins.AI.Api.Middleware;
using Bipins.AI.Core.Runtime.Policies;
using Bipins.AI.Runtime.Policies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;
using Xunit;

namespace Bipins.AI.UnitTests.Api;

public class MiddlewareTests
{
    private readonly Mock<ILogger<RateLimitMiddleware>> _middlewareLogger;
    private readonly Mock<ILoggerFactory> _loggerFactory;
    private readonly Mock<IRateLimiter> _rateLimiter;
    private readonly RateLimitingOptions _options;

    public MiddlewareTests()
    {
        _middlewareLogger = new Mock<ILogger<RateLimitMiddleware>>();
        _loggerFactory = new Mock<ILoggerFactory>();
        _loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);
        _rateLimiter = new Mock<IRateLimiter>();
        _options = new RateLimitingOptions
        {
            MaxRequestsPerWindow = 100,
            TimeWindow = TimeSpan.FromMinutes(1),
            MaxConcurrentRequests = 10
        };
    }

    [Fact]
    public async Task RateLimitMiddleware_HealthCheckEndpoint_BypassesRateLimit()
    {
        var middleware = new RateLimitMiddleware(
            async context => await Task.CompletedTask,
            _middlewareLogger.Object,
            _loggerFactory.Object,
            Options.Create(_options),
            _rateLimiter.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/health";
        httpContext.User = new System.Security.Claims.ClaimsPrincipal();

        await middleware.InvokeAsync(httpContext);

        // Should not call rate limiter
        _rateLimiter.Verify(r => r.TryAcquireAsync(
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RateLimitMiddleware_MetricsEndpoint_BypassesRateLimit()
    {
        var middleware = new RateLimitMiddleware(
            async context => await Task.CompletedTask,
            _middlewareLogger.Object,
            _loggerFactory.Object,
            Options.Create(_options),
            _rateLimiter.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/metrics";
        httpContext.User = new System.Security.Claims.ClaimsPrincipal();

        await middleware.InvokeAsync(httpContext);

        // Should not call rate limiter
        _rateLimiter.Verify(r => r.TryAcquireAsync(
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RateLimitMiddleware_RateLimitExceeded_Returns429()
    {
        var middleware = new RateLimitMiddleware(
            async context => await Task.CompletedTask,
            _middlewareLogger.Object,
            _loggerFactory.Object,
            Options.Create(_options),
            _rateLimiter.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/v1/chat";
        var claims = new System.Security.Claims.ClaimsIdentity();
        claims.AddClaim(new System.Security.Claims.Claim("tenantId", "tenant1"));
        httpContext.User = new System.Security.Claims.ClaimsPrincipal(claims);

        _rateLimiter.Setup(r => r.TryAcquireAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _rateLimiter.Setup(r => r.GetRetryAfterAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(60);

        await middleware.InvokeAsync(httpContext);

        Assert.Equal((int)HttpStatusCode.TooManyRequests, httpContext.Response.StatusCode);
        Assert.Equal("60", httpContext.Response.Headers["Retry-After"].ToString());
    }

    [Fact]
    public async Task RateLimitMiddleware_WithinRateLimit_Proceeds()
    {
        var wasCalled = false;
        var middleware = new RateLimitMiddleware(
            async context => { wasCalled = true; await Task.CompletedTask; },
            _middlewareLogger.Object,
            _loggerFactory.Object,
            Options.Create(_options),
            _rateLimiter.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/v1/chat";
        var claims = new System.Security.Claims.ClaimsIdentity();
        claims.AddClaim(new System.Security.Claims.Claim("tenantId", "tenant1"));
        httpContext.User = new System.Security.Claims.ClaimsPrincipal(claims);

        _rateLimiter.Setup(r => r.TryAcquireAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await middleware.InvokeAsync(httpContext);

        Assert.True(wasCalled);
        Assert.NotEqual((int)HttpStatusCode.TooManyRequests, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task RateLimitMiddleware_WithoutTenantId_UsesDefault()
    {
        var middleware = new RateLimitMiddleware(
            async context => await Task.CompletedTask,
            _middlewareLogger.Object,
            _loggerFactory.Object,
            Options.Create(_options),
            _rateLimiter.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/v1/chat";
        httpContext.User = new System.Security.Claims.ClaimsPrincipal();

        _rateLimiter.Setup(r => r.TryAcquireAsync(
                It.Is<string>(key => key.Contains("default")),
                It.IsAny<int>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await middleware.InvokeAsync(httpContext);

        _rateLimiter.Verify(r => r.TryAcquireAsync(
            It.Is<string>(key => key.Contains("default")),
            It.IsAny<int>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
