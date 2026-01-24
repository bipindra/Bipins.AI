using Bipins.AI.Core.Models;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Bipins.AI.Api.HealthChecks;

/// <summary>
/// Health check for chat model connectivity.
/// </summary>
public class ChatModelHealthCheck : IHealthCheck
{
    private readonly IChatModel _chatModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatModelHealthCheck"/> class.
    /// </summary>
    public ChatModelHealthCheck(IChatModel chatModel)
    {
        _chatModel = chatModel;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Try a minimal request to check connectivity
            var testRequest = new ChatRequest(
                new[] { new Message(MessageRole.User, "test") },
                MaxTokens: 1);
            
            // Use a timeout to avoid long waits
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            
            await _chatModel.GenerateAsync(testRequest, linkedCts.Token);
            
            return HealthCheckResult.Healthy("Chat model is accessible");
        }
        catch (OperationCanceledException)
        {
            return HealthCheckResult.Degraded("Chat model health check timed out");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Chat model is not accessible", ex);
        }
    }
}
