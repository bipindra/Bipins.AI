using Bipins.AI.Runtime.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bipins.AI.UnitTests.Runtime;

public class CacheTests
{
    [Fact]
    public void DistributedCacheAdapter_CanBeInstantiated()
    {
        var mockDistributedCache = new Mock<IDistributedCache>();
        var logger = new Mock<ILogger<DistributedCacheAdapter>>();
        var cache = new DistributedCacheAdapter(mockDistributedCache.Object, logger.Object);

        Assert.NotNull(cache);
    }

    [Fact]
    public async Task DistributedCacheAdapter_SetAsync_StoresValue()
    {
        var mockDistributedCache = new Mock<IDistributedCache>();
        var logger = new Mock<ILogger<DistributedCacheAdapter>>();
        var cache = new DistributedCacheAdapter(mockDistributedCache.Object, logger.Object);

        await cache.SetAsync("test-key", "test-value", TimeSpan.FromMinutes(5));

        mockDistributedCache.Verify(
            d => d.SetAsync(
                It.Is<string>(k => k.Contains("test-key")),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DistributedCacheAdapter_GetAsync_WithNonExistentKey_ReturnsNull()
    {
        var mockDistributedCache = new Mock<IDistributedCache>();
        mockDistributedCache.Setup(d => d.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        var logger = new Mock<ILogger<DistributedCacheAdapter>>();
        var cache = new DistributedCacheAdapter(mockDistributedCache.Object, logger.Object);

        var result = await cache.GetAsync<string>("non-existent-key");

        Assert.Null(result);
    }

    [Fact]
    public async Task DistributedCacheAdapter_GetAsync_WithExistingKey_ReturnsValue()
    {
        var testValue = "test-value";
        var jsonBytes = System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(testValue));

        var mockDistributedCache = new Mock<IDistributedCache>();
        mockDistributedCache.Setup(d => d.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(jsonBytes);

        var logger = new Mock<ILogger<DistributedCacheAdapter>>();
        var cache = new DistributedCacheAdapter(mockDistributedCache.Object, logger.Object);

        var result = await cache.GetAsync<string>("test-key");

        Assert.Equal(testValue, result);
    }
}
