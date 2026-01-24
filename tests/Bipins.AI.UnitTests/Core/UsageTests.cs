using Bipins.AI.Core.Models;
using Xunit;

namespace Bipins.AI.UnitTests.Core;

public class UsageTests
{
    [Fact]
    public void Usage_WithAllTokens_CreatesSuccessfully()
    {
        var usage = new Usage(100, 50, 150);

        Assert.Equal(100, usage.PromptTokens);
        Assert.Equal(50, usage.CompletionTokens);
        Assert.Equal(150, usage.TotalTokens);
    }

    [Fact]
    public void Usage_TotalTokens_MatchesSum()
    {
        var usage = new Usage(200, 300, 500);

        Assert.Equal(500, usage.TotalTokens);
        Assert.Equal(usage.PromptTokens + usage.CompletionTokens, usage.TotalTokens);
    }

    [Fact]
    public void Usage_WithZeroTokens_HandlesCorrectly()
    {
        var usage = new Usage(0, 0, 0);

        Assert.Equal(0, usage.PromptTokens);
        Assert.Equal(0, usage.CompletionTokens);
        Assert.Equal(0, usage.TotalTokens);
    }
}
