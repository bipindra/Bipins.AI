using Bipins.AI.Resilience;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bipins.AI.UnitTests.Resilience;

public class PollyResiliencePolicyTests
{
    private readonly Mock<ILogger<PollyResiliencePolicy>> _mockLogger;

    public PollyResiliencePolicyTests()
    {
        _mockLogger = new Mock<ILogger<PollyResiliencePolicy>>();
    }

    [Fact]
    public async Task ExecuteAsync_WithRetryPolicy_RetriesOnFailure()
    {
        var options = new ResilienceOptions
        {
            Retry = new RetryOptions
            {
                MaxRetries = 2,
                Delay = TimeSpan.FromMilliseconds(10),
                BackoffStrategy = BackoffStrategy.Fixed
            }
        };

        var policy = new PollyResiliencePolicy(options, _mockLogger.Object);

        int attemptCount = 0;
        await policy.ExecuteAsync(async () =>
        {
            attemptCount++;
            if (attemptCount < 3)
            {
                throw new InvalidOperationException("Transient error");
            }
            await Task.CompletedTask;
        });

        Assert.Equal(3, attemptCount);
    }

    [Fact]
    public async Task ExecuteAsync_WithTimeoutPolicy_ThrowsOnTimeout()
    {
        var options = new ResilienceOptions
        {
            Timeout = new TimeoutOptions
            {
                Timeout = TimeSpan.FromMilliseconds(50)
            }
        };

        var policy = new PollyResiliencePolicy(options, _mockLogger.Object);

        await Assert.ThrowsAsync<Polly.Timeout.TimeoutRejectedException>(async () =>
        {
            await policy.ExecuteAsync(async () =>
            {
                await Task.Delay(200);
            });
        });
    }

    [Fact]
    public async Task ExecuteAsync_WithBulkheadPolicy_LimitsConcurrency()
    {
        var options = new ResilienceOptions
        {
            Bulkhead = new BulkheadOptions
            {
                MaxParallelization = 2,
                MaxQueuingActions = 1
            }
        };

        var policy = new PollyResiliencePolicy(options, _mockLogger.Object);

        int concurrentExecutions = 0;
        int maxConcurrent = 0;

        var tasks = Enumerable.Range(0, 5).Select(async i =>
        {
            await policy.ExecuteAsync(async () =>
            {
                var current = Interlocked.Increment(ref concurrentExecutions);
                maxConcurrent = Math.Max(maxConcurrent, current);
                await Task.Delay(50);
                Interlocked.Decrement(ref concurrentExecutions);
            });
        });

        await Task.WhenAll(tasks);

        Assert.True(maxConcurrent <= 2);
    }

    [Fact]
    public async Task ExecuteAsync_WithFunction_ReturnsResult()
    {
        var options = new ResilienceOptions();
        var policy = new PollyResiliencePolicy(options, _mockLogger.Object);

        var result = await policy.ExecuteAsync(async () =>
        {
            await Task.CompletedTask;
            return 42;
        });

        Assert.Equal(42, result);
    }
}
