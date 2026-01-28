using Bipins.AI.Core;
using Xunit;

namespace Bipins.AI.UnitTests.Core;

public class SnakeCaseLowerNamingPolicyTests
{
    [Theory]
    [InlineData("FirstName", "first_name")]
    [InlineData("UserId", "user_id")]
    [InlineData("URLValue", "url_value")]
    [InlineData("simple", "simple")]
    [InlineData("already_snake_case", "already_snake_case")]
    [InlineData("Value1", "value1")]
    public void ConvertName_ConvertsToSnakeCaseLower(string input, string expected)
    {
        var result = SnakeCaseLowerNamingPolicy.Instance.ConvertName(input);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ConvertName_NullOrEmpty_ReturnsInput()
    {
        Assert.Equal(string.Empty, SnakeCaseLowerNamingPolicy.Instance.ConvertName(string.Empty));
        Assert.Null(SnakeCaseLowerNamingPolicy.Instance.ConvertName(null!));
    }
}

