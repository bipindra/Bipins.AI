using Bipins.AI.Runtime.Policies;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace Bipins.AI.UnitTests.Runtime;

public class DistributedRateLimiterTests
{
    private readonly Mock<ILogger<DistributedRateLimiter>> _logger;
    private readonly Mock<IConnectionMultiplexer> _mockRedis;
    private readonly Mock<IDatabase> _mockDatabase;

    public DistributedRateLimiterTests()
    {
        _logger = new Mock<ILogger<DistributedRateLimiter>>();
        _mockRedis = new Mock<IConnectionMultiplexer>();
        _mockDatabase = new Mock<IDatabase>();
        
        _mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_mockDatabase.Object);
    }

    [Fact]
    public void DistributedRateLimiter_WithoutRedis_UsesFallback()
    {
        var limiter = new DistributedRateLimiter(_logger.Object, null);

        Assert.NotNull(limiter);
    }

    [Fact]
    public void DistributedRateLimiter_WithRedis_CreatesSuccessfully()
    {
        var limiter = new DistributedRateLimiter(_logger.Object, _mockRedis.Object);

        Assert.NotNull(limiter);
    }

    [Fact]
    public async Task TryAcquireAsync_WithoutRedis_AllowsRequest()
    {
        var limiter = new DistributedRateLimiter(_logger.Object, null);
        var result = await limiter.TryAcquireAsync("test-key", 5, TimeSpan.FromSeconds(10));

        Assert.True(result);
    }

    [Fact]
    public async Task GetRetryAfterAsync_WithoutRedis_ReturnsNull()
    {
        var limiter = new DistributedRateLimiter(_logger.Object, null);
        var result = await limiter.GetRetryAfterAsync("test-key", 5, TimeSpan.FromSeconds(10));

        Assert.Null(result);
    }

    [Fact]
    public async Task ResetAsync_WithoutRedis_Completes()
    {
        var limiter = new DistributedRateLimiter(_logger.Object, null);
        await limiter.ResetAsync("test-key");

        // Should complete without exception
        Assert.True(true);
    }
}
