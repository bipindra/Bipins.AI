using Bipins.AI.Api.HealthChecks;
using Bipins.AI.Core.Models;
using Bipins.AI.Vector;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using Xunit;

namespace Bipins.AI.UnitTests.Api;

public class HealthCheckTests
{
    private readonly Mock<IVectorStore> _vectorStore;

    public HealthCheckTests()
    {
        _vectorStore = new Mock<IVectorStore>();
    }

    [Fact]
    public async Task ChatModelHealthCheck_Healthy_ReturnsHealthy()
    {
        var chatModel = new Mock<IChatModel>();
        var healthCheck = new ChatModelHealthCheck(chatModel.Object);

        chatModel.Setup(m => m.GenerateAsync(
                It.IsAny<ChatRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse("OK"));

        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("test", healthCheck, null, null)
        };

        var result = await healthCheck.CheckHealthAsync(context, CancellationToken.None);

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task ChatModelHealthCheck_Unhealthy_ReturnsUnhealthy()
    {
        var chatModel = new Mock<IChatModel>();
        var healthCheck = new ChatModelHealthCheck(chatModel.Object);

        chatModel.Setup(m => m.GenerateAsync(
                It.IsAny<ChatRequest>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Model unavailable"));

        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("test", healthCheck, null, null)
        };

        var result = await healthCheck.CheckHealthAsync(context, CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task VectorStoreHealthCheck_Healthy_ReturnsHealthy()
    {
        var healthCheck = new VectorStoreHealthCheck(_vectorStore.Object);

        var queryResponse = new VectorQueryResponse(Array.Empty<VectorMatch>());

        _vectorStore.Setup(v => v.QueryAsync(
                It.IsAny<VectorQueryRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryResponse);

        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("test", healthCheck, null, null)
        };

        var result = await healthCheck.CheckHealthAsync(context, CancellationToken.None);

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task VectorStoreHealthCheck_Unhealthy_ReturnsUnhealthy()
    {
        var healthCheck = new VectorStoreHealthCheck(_vectorStore.Object);

        _vectorStore.Setup(v => v.QueryAsync(
                It.IsAny<VectorQueryRequest>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Vector store unavailable"));

        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("test", healthCheck, null, null)
        };

        var result = await healthCheck.CheckHealthAsync(context, CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }
}
