using Bipins.AI.Runtime.Pipeline;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bipins.AI.UnitTests.Runtime;

public class PipelineTests
{
    private readonly Mock<ILogger<PipelineRunner>> _runnerLogger;
    private readonly Mock<ILogger<StepRetryHandler>> _retryLogger;
    private readonly StepRetryHandler _retryHandler;
    private readonly PipelineRunner _runner;

    public PipelineTests()
    {
        _runnerLogger = new Mock<ILogger<PipelineRunner>>();
        _retryLogger = new Mock<ILogger<StepRetryHandler>>();
        _retryHandler = new StepRetryHandler(_retryLogger.Object);
        _runner = new PipelineRunner(_runnerLogger.Object, _retryHandler);
    }

    [Fact]
    public void PipelineContext_Create_SetsProperties()
    {
        var context = PipelineContext.Create("tenant1", "corr1");

        Assert.Equal("tenant1", context.TenantId);
        Assert.Equal("corr1", context.CorrelationId);
        Assert.NotNull(context.Stopwatch);
        Assert.NotNull(context.Tags);
        Assert.Null(context.Claims);
        Assert.Null(context.Policy);
        Assert.Null(context.Activity);
    }

    [Fact]
    public void PipelineContext_WithAllProperties_SetsProperties()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var tags = new Dictionary<string, object> { { "key", "value" } };
        var claims = new Dictionary<string, string> { { "claim1", "value1" } };
        var context = new PipelineContext("tenant1", "corr1", stopwatch, tags, claims);

        Assert.Equal("tenant1", context.TenantId);
        Assert.Equal("corr1", context.CorrelationId);
        Assert.Equal(stopwatch, context.Stopwatch);
        Assert.Equal(tags, context.Tags);
        Assert.Equal(claims, context.Claims);
    }

    [Fact]
    public void RetryPolicy_DefaultValues_AreCorrect()
    {
        var policy = new RetryPolicy();

        Assert.Equal(3, policy.MaxAttempts);
        Assert.Equal(1000, policy.InitialDelay);
        Assert.Equal(30000, policy.MaxDelay);
        Assert.Equal(2.0, policy.BackoffMultiplier);
    }

    [Fact]
    public void RetryPolicy_CustomValues_AreSet()
    {
        var policy = new RetryPolicy(
            MaxAttempts: 5,
            InitialDelay: 500,
            MaxDelay: 10000,
            BackoffMultiplier: 1.5);

        Assert.Equal(5, policy.MaxAttempts);
        Assert.Equal(500, policy.InitialDelay);
        Assert.Equal(10000, policy.MaxDelay);
        Assert.Equal(1.5, policy.BackoffMultiplier);
    }

    [Fact]
    public async Task PipelineRunner_ExecuteAsync_WithSingleStep_ExecutesStep()
    {
        var step = new Mock<IPipelineStep<string, string>>();
        var context = PipelineContext.Create("tenant1", "corr1");

        step.Setup(s => s.ExecuteAsync("input", context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PipelineStepResult<string>(true, "output"));

        var steps = new[] { step.Object };
        var result = await _runner.ExecuteAsync("input", context, steps);

        Assert.True(result.Success);
        Assert.Equal("output", result.Value);
    }

    [Fact]
    public async Task PipelineRunner_ExecuteAsync_WithMultipleSteps_ExecutesSequentially()
    {
        var step1 = new Mock<IPipelineStep<string, string>>();
        var step2 = new Mock<IPipelineStep<string, string>>();
        var context = PipelineContext.Create("tenant1", "corr1");

        step1.Setup(s => s.ExecuteAsync("input", context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PipelineStepResult<string>(true, "intermediate"));

        step2.Setup(s => s.ExecuteAsync("intermediate", context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PipelineStepResult<string>(true, "output"));

        var steps = new IPipelineStep<string, string>[] { step1.Object, step2.Object };
        var result = await _runner.ExecuteAsync("input", context, steps);

        Assert.True(result.Success);
        Assert.Equal("output", result.Value);
    }

    [Fact]
    public async Task PipelineRunner_ExecuteAsync_WithNoSteps_ReturnsFailure()
    {
        var context = PipelineContext.Create("tenant1", "corr1");
        var steps = Array.Empty<IPipelineStep<string, string>>();

        var result = await _runner.ExecuteAsync("input", context, steps);

        Assert.False(result.Success);
        Assert.Equal("No steps provided", result.Error);
    }

    [Fact]
    public async Task PipelineRunner_ExecuteAsync_WithFailingStep_ReturnsFailure()
    {
        var step = new Mock<IPipelineStep<string, string>>();
        var context = PipelineContext.Create("tenant1", "corr1");

        step.Setup(s => s.ExecuteAsync("input", context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PipelineStepResult<string>(false, null, "Step failed"));

        var steps = new[] { step.Object };
        var result = await _runner.ExecuteAsync("input", context, steps);

        Assert.False(result.Success);
        Assert.Equal("Step failed", result.Error);
    }

    [Fact]
    public async Task PipelineRunner_ExecuteAsync_WithRetryPolicy_RetriesOnFailure()
    {
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
        var steps = new[] { step.Object };
        var result = await _runner.ExecuteAsync("input", context, steps, retryPolicy);

        Assert.True(result.Success);
        Assert.Equal("output", result.Value);
        Assert.True(attempt >= 2);
    }

    [Fact]
    public async Task PipelineRunner_ExecuteAsync_WithTimeout_TimesOut()
    {
        var step = new Mock<IPipelineStep<string, string>>();
        var context = PipelineContext.Create("tenant1", "corr1");

        step.Setup(s => s.ExecuteAsync("input", context, It.IsAny<CancellationToken>()))
            .Returns(async (string input, PipelineContext ctx, CancellationToken ct) =>
            {
                await Task.Delay(2000, ct);
                return new PipelineStepResult<string>(true, "output");
            });

        var steps = new[] { step.Object };
        var result = await _runner.ExecuteAsync("input", context, steps, timeout: TimeSpan.FromMilliseconds(100));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task PipelineRunner_ExecuteAsync_PropagatesContext()
    {
        var step = new Mock<IPipelineStep<string, string>>();
        var context = PipelineContext.Create("tenant1", "corr1");
        PipelineContext? capturedContext = null;

        step.Setup(s => s.ExecuteAsync("input", It.IsAny<PipelineContext>(), It.IsAny<CancellationToken>()))
            .Callback<string, PipelineContext, CancellationToken>((input, ctx, ct) => capturedContext = ctx)
            .ReturnsAsync(new PipelineStepResult<string>(true, "output"));

        var steps = new[] { step.Object };
        await _runner.ExecuteAsync("input", context, steps);

        Assert.NotNull(capturedContext);
        Assert.Equal(context.TenantId, capturedContext.TenantId);
        Assert.Equal(context.CorrelationId, capturedContext.CorrelationId);
    }
}
