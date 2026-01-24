using Bipins.AI.Runtime.Policies;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bipins.AI.UnitTests;

public class PolicyProviderTests
{
    [Fact]
    public async Task GetPolicyAsync_WithTenantId_ReturnsTenantPolicy()
    {
        var logger = new Mock<ILogger<DefaultPolicyProvider>>();
        var provider = new DefaultPolicyProvider(logger.Object);

        provider.SetPolicy("tenant1", new AiPolicy(
            AllowedProviders: new[] { "OpenAI" },
            MaxTokens: 50000));

        var policy = await provider.GetPolicyAsync("tenant1");

        Assert.NotNull(policy);
        Assert.Equal(50000, policy.MaxTokens);
        Assert.Contains("OpenAI", policy.AllowedProviders);
    }

    [Fact]
    public async Task GetPolicyAsync_WithoutTenantId_ReturnsDefaultPolicy()
    {
        var logger = new Mock<ILogger<DefaultPolicyProvider>>();
        var provider = new DefaultPolicyProvider(logger.Object);

        var policy = await provider.GetPolicyAsync("unknown-tenant");

        Assert.NotNull(policy);
        // Should return default policy
        Assert.True(policy.AllowedProviders.Count > 0);
    }
}
