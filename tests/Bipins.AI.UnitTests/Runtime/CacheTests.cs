using Bipins.AI.Runtime.Caching;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace Bipins.AI.UnitTests.Runtime;

public class CacheTests
{
    [Fact]
    public void MemoryCache_CanBeInstantiated()
    {
        var logger = new Mock<ILogger<MemoryCache>>();
        var cache = new MemoryCache(logger.Object);

        Assert.NotNull(cache);
    }

    [Fact]
    public async Task MemoryCache_SetAsync_StoresValue()
    {
        var logger = new Mock<ILogger<MemoryCache>>();
        var cache = new MemoryCache(logger.Object);

        await cache.SetAsync("test-key", "test-value", TimeSpan.FromMinutes(5));

        var result = await cache.GetAsync<string>("test-key");
        Assert.Equal("test-value", result);
    }

    [Fact]
    public async Task MemoryCache_GetAsync_WithNonExistentKey_ReturnsNull()
    {
        var logger = new Mock<ILogger<MemoryCache>>();
        var cache = new MemoryCache(logger.Object);

        var result = await cache.GetAsync<string>("non-existent-key");

        Assert.Null(result);
    }

    [Fact]
    public async Task MemoryCache_GetAsync_WithExpiredKey_ReturnsNull()
    {
        var logger = new Mock<ILogger<MemoryCache>>();
        var cache = new MemoryCache(logger.Object);

        await cache.SetAsync("expired-key", "value", TimeSpan.FromMilliseconds(10));
        await Task.Delay(50); // Wait for expiration

        var result = await cache.GetAsync<string>("expired-key");
        Assert.Null(result);
    }

    [Fact]
    public async Task MemoryCache_GetAsync_WithWrongType_ReturnsNull()
    {
        var logger = new Mock<ILogger<MemoryCache>>();
        var cache = new MemoryCache(logger.Object);

        await cache.SetAsync("key", "string-value", TimeSpan.FromMinutes(5));

        var result = await cache.GetAsync<string>("key");
        Assert.Equal("string-value", result);
    }

    [Fact]
    public void RedisCache_CanBeInstantiated()
    {
        var mockRedis = new Mock<IConnectionMultiplexer>();
        var mockDatabase = new Mock<IDatabase>();
        mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(mockDatabase.Object);
        mockRedis.Setup(r => r.GetEndPoints()).Returns(Array.Empty<System.Net.EndPoint>());

        var logger = new Mock<ILogger<RedisCache>>();
        var cache = new RedisCache(mockRedis.Object, logger.Object);

        Assert.NotNull(cache);
    }

    [Fact]
    public async Task RedisCache_GetAsync_WithNonExistentKey_ReturnsNull()
    {
        var mockRedis = new Mock<IConnectionMultiplexer>();
        var mockDatabase = new Mock<IDatabase>();
        mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(mockDatabase.Object);

        mockDatabase.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        var logger = new Mock<ILogger<RedisCache>>();
        var cache = new RedisCache(mockRedis.Object, logger.Object);

        var result = await cache.GetAsync<string>("non-existent-key");

        Assert.Null(result);
    }
}
