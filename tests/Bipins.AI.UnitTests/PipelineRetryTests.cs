using Bipins.AI.Runtime.Pipeline;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bipins.AI.UnitTests;

public class PipelineRetryTests
{
    [Fact]
    public async Task ExecuteWithRetryAsync_SuccessfulStep_ReturnsResult()
    {
        var logger = new Mock<ILogger<StepRetryHandler>>();
        var handler = new StepRetryHandler(logger.Object);
        var step = new Mock<IPipelineStep<string, string>>();
        var context = PipelineContext.Create("tenant1", "corr1");

        step.Setup(s => s.ExecuteAsync("input", context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PipelineStepResult<string>(true, "output"));

        var result = await handler.ExecuteWithRetryAsync(step.Object, "input", context);

        Assert.True(result.Success);
        Assert.Equal("output", result.Value);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_FailingStep_Retries()
    {
        var logger = new Mock<ILogger<StepRetryHandler>>();
        var handler = new StepRetryHandler(logger.Object);
        var step = new Mock<IPipelineStep<string, string>>();
        var context = PipelineContext.Create("tenant1", "corr1");
        var attempt = 0;

        step.Setup(s => s.ExecuteAsync("input", context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                attempt++;
                if (attempt < 2)
                {
                    return new PipelineStepResult<string>(false, null, "Error");
                }

                return new PipelineStepResult<string>(true, "output");
            });

        var retryPolicy = new RetryPolicy(MaxAttempts: 3, InitialDelay: 10);
        var result = await handler.ExecuteWithRetryAsync(step.Object, "input", context, retryPolicy);

        Assert.True(result.Success);
        Assert.Equal("output", result.Value);
        Assert.True(attempt >= 2);
    }
}
