using Bipins.AI.Runtime.Policies;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bipins.AI.UnitTests.Runtime;

public class MemoryRateLimiterTests
{
    private readonly Mock<ILogger<MemoryRateLimiter>> _logger;

    public MemoryRateLimiterTests()
    {
        _logger = new Mock<ILogger<MemoryRateLimiter>>();
    }

    [Fact]
    public void MemoryRateLimiter_CanBeInstantiated()
    {
        var limiter = new MemoryRateLimiter(_logger.Object);

        Assert.NotNull(limiter);
    }

    [Fact]
    public async Task TryAcquireAsync_WithinLimit_ReturnsTrue()
    {
        var limiter = new MemoryRateLimiter(_logger.Object);
        var key = "test-key";
        var limit = 5;
        var window = TimeSpan.FromSeconds(10);

        // Make 5 requests within limit
        for (int i = 0; i < limit; i++)
        {
            var result = await limiter.TryAcquireAsync(key, limit, window);
            Assert.True(result);
        }
    }

    [Fact]
    public async Task TryAcquireAsync_ExceedsLimit_ReturnsFalse()
    {
        var limiter = new MemoryRateLimiter(_logger.Object);
        var key = "test-key";
        var limit = 3;
        var window = TimeSpan.FromSeconds(10);

        // Make requests up to limit
        for (int i = 0; i < limit; i++)
        {
            var result = await limiter.TryAcquireAsync(key, limit, window);
            Assert.True(result);
        }

        // Next request should be rejected
        var exceeded = await limiter.TryAcquireAsync(key, limit, window);
        Assert.False(exceeded);
    }

    [Fact]
    public async Task TryAcquireAsync_DifferentKeys_AreIndependent()
    {
        var limiter = new MemoryRateLimiter(_logger.Object);
        var limit = 2;
        var window = TimeSpan.FromSeconds(10);

        // Exhaust limit for key1
        await limiter.TryAcquireAsync("key1", limit, window);
        await limiter.TryAcquireAsync("key1", limit, window);
        var key1Exceeded = await limiter.TryAcquireAsync("key1", limit, window);
        Assert.False(key1Exceeded);

        // key2 should still work
        var key2Allowed = await limiter.TryAcquireAsync("key2", limit, window);
        Assert.True(key2Allowed);
    }

    [Fact]
    public async Task GetRetryAfterAsync_WhenLimitExceeded_ReturnsPositiveValue()
    {
        var limiter = new MemoryRateLimiter(_logger.Object);
        var key = "test-key";
        var limit = 2;
        var window = TimeSpan.FromSeconds(10);

        // Exhaust limit
        await limiter.TryAcquireAsync(key, limit, window);
        await limiter.TryAcquireAsync(key, limit, window);

        var retryAfter = await limiter.GetRetryAfterAsync(key, limit, window);
        Assert.True(retryAfter > 0);
    }

    [Fact]
    public async Task ResetAsync_ClearsLimit()
    {
        var limiter = new MemoryRateLimiter(_logger.Object);
        var key = "test-key";
        var limit = 2;
        var window = TimeSpan.FromSeconds(10);

        // Exhaust limit
        await limiter.TryAcquireAsync(key, limit, window);
        await limiter.TryAcquireAsync(key, limit, window);
        var exceeded = await limiter.TryAcquireAsync(key, limit, window);
        Assert.False(exceeded);

        // Reset
        await limiter.ResetAsync(key);

        // Should be able to acquire again
        var afterReset = await limiter.TryAcquireAsync(key, limit, window);
        Assert.True(afterReset);
    }
}
