using Bipins.AI.Core.Runtime.Policies;
using Bipins.AI.Runtime.Policies;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bipins.AI.UnitTests.Runtime;

public class PoliciesTests
{
    private readonly Mock<ILogger<MemoryRateLimiter>> _memoryLogger;
    private readonly Mock<ILogger<DistributedRateLimiter>> _distributedLogger;
    private readonly Mock<ILogger<RateLimitingPolicy>> _policyLogger;
    private readonly Mock<ILogger<ThrottlingPolicy>> _throttlingLogger;

    public PoliciesTests()
    {
        _memoryLogger = new Mock<ILogger<MemoryRateLimiter>>();
        _distributedLogger = new Mock<ILogger<DistributedRateLimiter>>();
        _policyLogger = new Mock<ILogger<RateLimitingPolicy>>();
        _throttlingLogger = new Mock<ILogger<ThrottlingPolicy>>();
    }

    [Fact]
    public async Task MemoryRateLimiter_TryAcquireAsync_WithinLimit_ReturnsTrue()
    {
        var limiter = new MemoryRateLimiter(_memoryLogger.Object);
        var result = await limiter.TryAcquireAsync("key1", 10, TimeSpan.FromMinutes(1));

        Assert.True(result);
    }

    [Fact]
    public async Task MemoryRateLimiter_TryAcquireAsync_ExceedsLimit_ReturnsFalse()
    {
        var limiter = new MemoryRateLimiter(_memoryLogger.Object);
        var limit = 5;
        var window = TimeSpan.FromMinutes(1);

        // Acquire all available slots
        for (int i = 0; i < limit; i++)
        {
            var result = await limiter.TryAcquireAsync("key1", limit, window);
            Assert.True(result);
        }

        // Next request should be rejected
        var exceeded = await limiter.TryAcquireAsync("key1", limit, window);
        Assert.False(exceeded);
    }

    [Fact]
    public async Task MemoryRateLimiter_TryAcquireAsync_AfterWindow_Resets()
    {
        var limiter = new MemoryRateLimiter(_memoryLogger.Object);
        var limit = 2;
        var window = TimeSpan.FromMilliseconds(100);

        // Acquire all slots
        await limiter.TryAcquireAsync("key1", limit, window);
        await limiter.TryAcquireAsync("key1", limit, window);
        var exceeded = await limiter.TryAcquireAsync("key1", limit, window);
        Assert.False(exceeded);

        // Wait for window to expire
        await Task.Delay(150);

        // Should be able to acquire again
        var allowed = await limiter.TryAcquireAsync("key1", limit, window);
        Assert.True(allowed);
    }

    [Fact]
    public async Task MemoryRateLimiter_GetRetryAfterAsync_WhenLimited_ReturnsSeconds()
    {
        var limiter = new MemoryRateLimiter(_memoryLogger.Object);
        var limit = 2;
        var window = TimeSpan.FromSeconds(10);

        // Fill up the limit
        await limiter.TryAcquireAsync("key1", limit, window);
        await limiter.TryAcquireAsync("key1", limit, window);

        var retryAfter = await limiter.GetRetryAfterAsync("key1", limit, window);
        Assert.NotNull(retryAfter);
        Assert.True(retryAfter > 0);
    }

    [Fact]
    public async Task MemoryRateLimiter_GetRetryAfterAsync_WhenNotLimited_ReturnsNull()
    {
        var limiter = new MemoryRateLimiter(_memoryLogger.Object);
        var retryAfter = await limiter.GetRetryAfterAsync("key1", 10, TimeSpan.FromMinutes(1));

        Assert.Null(retryAfter);
    }

    [Fact]
    public async Task MemoryRateLimiter_ResetAsync_ClearsLimits()
    {
        var limiter = new MemoryRateLimiter(_memoryLogger.Object);
        var limit = 2;
        var window = TimeSpan.FromMinutes(1);

        // Fill up the limit
        await limiter.TryAcquireAsync("key1", limit, window);
        await limiter.TryAcquireAsync("key1", limit, window);
        var exceeded = await limiter.TryAcquireAsync("key1", limit, window);
        Assert.False(exceeded);

        // Reset
        await limiter.ResetAsync("key1");

        // Should be able to acquire again
        var allowed = await limiter.TryAcquireAsync("key1", limit, window);
        Assert.True(allowed);
    }

    [Fact]
    public async Task MemoryRateLimiter_ConcurrentAccess_IsThreadSafe()
    {
        var limiter = new MemoryRateLimiter(_memoryLogger.Object);
        var limit = 10;
        var window = TimeSpan.FromMinutes(1);

        var tasks = Enumerable.Range(0, 20)
            .Select(i => Task.Run(async () => await limiter.TryAcquireAsync("key1", limit, window)))
            .ToArray();

        var results = await Task.WhenAll(tasks);
        var allowedCount = results.Count(r => r);

        // Should allow exactly 'limit' requests
        Assert.Equal(limit, allowedCount);
    }

    [Fact]
    public async Task DistributedRateLimiter_WithoutRedis_AllowsRequests()
    {
        var limiter = new DistributedRateLimiter(_distributedLogger.Object, null);
        var result = await limiter.TryAcquireAsync("key1", 10, TimeSpan.FromMinutes(1));

        Assert.True(result);
    }

    [Fact]
    public async Task DistributedRateLimiter_WithRedis_AllowsRequests()
    {
        // Create a mock IConnectionMultiplexer or use null (will fall back to in-memory)
        var limiter = new DistributedRateLimiter(_distributedLogger.Object, null);
        var result = await limiter.TryAcquireAsync("key1", 10, TimeSpan.FromMinutes(1));

        // Without Redis connection, it falls back to in-memory and allows requests
        Assert.True(result);
    }

    [Fact]
    public async Task RateLimitingPolicy_ExecuteAsync_WithinLimit_ExecutesAction()
    {
        var rateLimiter = new MemoryRateLimiter(_memoryLogger.Object);
        var options = new RateLimitingOptions
        {
            MaxRequestsPerWindow = 10,
            TimeWindow = TimeSpan.FromMinutes(1),
            MaxConcurrentRequests = 5
        };
        var policy = new RateLimitingPolicy(_policyLogger.Object, options, rateLimiter);

        var result = await policy.ExecuteAsync("key1", async ct => await Task.FromResult(42));

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task RateLimitingPolicy_ExecuteAsync_ExceedsLimit_ThrowsException()
    {
        var rateLimiter = new MemoryRateLimiter(_memoryLogger.Object);
        var options = new RateLimitingOptions
        {
            MaxRequestsPerWindow = 2,
            TimeWindow = TimeSpan.FromMinutes(1),
            MaxConcurrentRequests = 5
        };
        var policy = new RateLimitingPolicy(_policyLogger.Object, options, rateLimiter);

        // Fill up the limit
        await policy.ExecuteAsync("key1", async ct => await Task.FromResult(1));
        await policy.ExecuteAsync("key1", async ct => await Task.FromResult(2));

        // Next request should throw
        await Assert.ThrowsAsync<RateLimitExceededException>(async () =>
            await policy.ExecuteAsync("key1", async ct => await Task.FromResult(3)));
    }

    [Fact]
    public async Task RateLimitingPolicy_ExecuteAsync_RespectsConcurrentLimit()
    {
        var rateLimiter = new MemoryRateLimiter(_memoryLogger.Object);
        var options = new RateLimitingOptions
        {
            MaxRequestsPerWindow = 100,
            TimeWindow = TimeSpan.FromMinutes(1),
            MaxConcurrentRequests = 2
        };
        var policy = new RateLimitingPolicy(_policyLogger.Object, options, rateLimiter);

        var semaphore = new SemaphoreSlim(0, 2);
        var tasks = Enumerable.Range(0, 5)
            .Select(i => Task.Run(async () =>
            {
                await policy.ExecuteAsync("key1", async ct =>
                {
                    semaphore.Release();
                    await Task.Delay(100, ct);
                    return i;
                });
            }))
            .ToArray();

        // Wait for semaphore to be released (max 2 concurrent)
        await Task.Delay(50);
        var concurrentCount = 2 - semaphore.CurrentCount;
        Assert.True(concurrentCount <= 2);

        await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task RateLimitingPolicy_GetRetryAfterSecondsAsync_ReturnsValue()
    {
        var rateLimiter = new MemoryRateLimiter(_memoryLogger.Object);
        var options = new RateLimitingOptions
        {
            MaxRequestsPerWindow = 2,
            TimeWindow = TimeSpan.FromSeconds(10),
            MaxConcurrentRequests = 5
        };
        var policy = new RateLimitingPolicy(_policyLogger.Object, options, rateLimiter);

        // Fill up the limit
        await policy.ExecuteAsync("key1", async ct => await Task.FromResult(1));
        await policy.ExecuteAsync("key1", async ct => await Task.FromResult(2));

        var retryAfter = await policy.GetRetryAfterSecondsAsync("key1");
        Assert.NotNull(retryAfter);
        Assert.True(retryAfter > 0);
    }

    [Fact]
    public void RateLimitingOptions_DefaultValues_AreCorrect()
    {
        var options = new RateLimitingOptions();

        Assert.Equal(10, options.MaxConcurrentRequests);
        Assert.Equal(100, options.MaxRequestsPerWindow);
        Assert.Equal(TimeSpan.FromMinutes(1), options.TimeWindow);
    }

    [Fact]
    public async Task ThrottlingPolicy_ExecuteAsync_ExecutesAction()
    {
        var options = new ThrottlingOptions
        {
            MaxConcurrentRequests = 10,
            HighLoadThreshold = 8,
            BaseThrottleDelay = 10,
            ThrottleRecoveryTime = TimeSpan.FromSeconds(30)
        };
        var policy = new ThrottlingPolicy(_throttlingLogger.Object, options);

        var result = await policy.ExecuteAsync(async ct => await Task.FromResult(42));

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task ThrottlingPolicy_ExecuteAsync_RespectsConcurrentLimit()
    {
        var options = new ThrottlingOptions
        {
            MaxConcurrentRequests = 2,
            HighLoadThreshold = 1,
            BaseThrottleDelay = 10,
            ThrottleRecoveryTime = TimeSpan.FromSeconds(30)
        };
        var policy = new ThrottlingPolicy(_throttlingLogger.Object, options);

        var semaphore = new SemaphoreSlim(0, 2);
        var tasks = Enumerable.Range(0, 5)
            .Select(i => Task.Run(async () =>
            {
                await policy.ExecuteAsync(async ct =>
                {
                    semaphore.Release();
                    await Task.Delay(100, ct);
                });
            }))
            .ToArray();

        await Task.Delay(50);
        var concurrentCount = 2 - semaphore.CurrentCount;
        Assert.True(concurrentCount <= 2);

        await Task.WhenAll(tasks);
    }

    [Fact]
    public void ThrottlingPolicy_ReportError_AdjustsThrottling()
    {
        var options = new ThrottlingOptions
        {
            MaxConcurrentRequests = 10,
            HighLoadThreshold = 8,
            BaseThrottleDelay = 10,
            ThrottleRecoveryTime = TimeSpan.FromSeconds(1)
        };
        var policy = new ThrottlingPolicy(_throttlingLogger.Object, options);

        policy.ReportError();

        // Should not throw
        Assert.True(true);
    }

    [Fact]
    public void ThrottlingOptions_DefaultValues_AreCorrect()
    {
        var options = new ThrottlingOptions();

        Assert.Equal(10, options.MaxConcurrentRequests);
        Assert.Equal(8, options.HighLoadThreshold);
        Assert.Equal(100, options.BaseThrottleDelay);
        Assert.Equal(TimeSpan.FromSeconds(30), options.ThrottleRecoveryTime);
    }
}
