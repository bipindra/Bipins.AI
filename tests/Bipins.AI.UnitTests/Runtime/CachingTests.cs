using Bipins.AI.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bipins.AI.UnitTests.Runtime;

public class CachingTests
{
    private readonly Mock<IDistributedCache> _mockDistributedCache;
    private readonly Mock<ILogger<DistributedCacheAdapter>> _logger;
    private readonly DistributedCacheAdapter _cache;

    public CachingTests()
    {
        _mockDistributedCache = new Mock<IDistributedCache>();
        _logger = new Mock<ILogger<DistributedCacheAdapter>>();
        _cache = new DistributedCacheAdapter(_mockDistributedCache.Object, _logger.Object);
    }

    [Fact]
    public async Task DistributedCacheAdapter_SetAsync_StoresValue()
    {
        await _cache.SetAsync("key1", "value1");

        _mockDistributedCache.Verify(
            d => d.SetAsync(
                It.Is<string>(k => k.Contains("key1")),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DistributedCacheAdapter_GetAsync_NonExistentKey_ReturnsNull()
    {
        _mockDistributedCache.Setup(d => d.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        var result = await _cache.GetAsync<string>("nonexistent");

        Assert.Null(result);
    }

    [Fact]
    public async Task DistributedCacheAdapter_SetAsync_WithTtl_SetsExpiration()
    {
        await _cache.SetAsync("key1", "value1", TimeSpan.FromMinutes(5));

        _mockDistributedCache.Verify(
            d => d.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.Is<DistributedCacheEntryOptions>(o => o.AbsoluteExpirationRelativeToNow == TimeSpan.FromMinutes(5)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DistributedCacheAdapter_SetAsync_WithoutTtl_UsesDefaultTtl()
    {
        await _cache.SetAsync("key1", "value1");

        _mockDistributedCache.Verify(
            d => d.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.Is<DistributedCacheEntryOptions>(o => o.AbsoluteExpirationRelativeToNow == TimeSpan.FromHours(1)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DistributedCacheAdapter_GetAsync_WithExistingKey_ReturnsValue()
    {
        var testValue = "value1";
        var jsonBytes = System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(testValue));

        _mockDistributedCache.Setup(d => d.GetAsync(It.Is<string>(k => k.Contains("key1")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(jsonBytes);

        var result = await _cache.GetAsync<string>("key1");
        Assert.Equal(testValue, result);
    }

    [Fact]
    public void CacheOptions_DefaultValues_AreCorrect()
    {
        var options = new CacheOptions();

        Assert.Equal(TimeSpan.FromHours(1), options.DefaultTtl);
        Assert.Equal("bipins:cache:", options.KeyPrefix);
    }

    [Fact]
    public void CacheOptions_CustomValues_AreSet()
    {
        var options = new CacheOptions
        {
            DefaultTtl = TimeSpan.FromMinutes(30),
            KeyPrefix = "custom:"
        };

        Assert.Equal(TimeSpan.FromMinutes(30), options.DefaultTtl);
        Assert.Equal("custom:", options.KeyPrefix);
    }
}
