using Bipins.AI.Core.Models;
using Xunit;

namespace Bipins.AI.UnitTests.Core;

public class SafetyInfoTests
{
    [Fact]
    public void SafetyInfo_WithAllProperties_CreatesSuccessfully()
    {
        var categories = new Dictionary<string, bool>
        {
            { "hate", true },
            { "violence", false }
        };
        var safety = new SafetyInfo(true, categories);

        Assert.True(safety.Flagged);
        Assert.NotNull(safety.Categories);
        Assert.True(safety.Categories["hate"]);
        Assert.False(safety.Categories["violence"]);
    }

    [Fact]
    public void SafetyInfo_WithMinimalProperties_CreatesSuccessfully()
    {
        var safety = new SafetyInfo(false, null);

        Assert.False(safety.Flagged);
        Assert.Null(safety.Categories);
    }
}
