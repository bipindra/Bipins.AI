using Bipins.AI.Core.Models;
using System.Text.Json;
using Xunit;

namespace Bipins.AI.UnitTests.Core;

public class ToolCallTests
{
    [Fact]
    public void ToolCall_WithAllProperties_CreatesSuccessfully()
    {
        var arguments = JsonSerializer.SerializeToElement(new { location = "Seattle", unit = "celsius" });
        var toolCall = new ToolCall("call_123", "get_weather", arguments);

        Assert.Equal("call_123", toolCall.Id);
        Assert.Equal("get_weather", toolCall.Name);
        Assert.True(toolCall.Arguments.ValueKind == JsonValueKind.Object);
        Assert.Equal("Seattle", toolCall.Arguments.GetProperty("location").GetString());
        Assert.Equal("celsius", toolCall.Arguments.GetProperty("unit").GetString());
    }

    [Fact]
    public void ToolCall_WithEmptyArguments_HandlesCorrectly()
    {
        var arguments = JsonSerializer.SerializeToElement(new { });
        var toolCall = new ToolCall("call_1", "no_args_tool", arguments);

        Assert.Equal("call_1", toolCall.Id);
        Assert.Equal("no_args_tool", toolCall.Name);
        Assert.True(toolCall.Arguments.ValueKind == JsonValueKind.Object);
    }

    [Fact]
    public void ToolCall_WithArrayArguments_HandlesCorrectly()
    {
        var arguments = JsonSerializer.SerializeToElement(new[] { "item1", "item2" });
        var toolCall = new ToolCall("call_2", "process_items", arguments);

        Assert.True(toolCall.Arguments.ValueKind == JsonValueKind.Array);
        Assert.Equal(2, toolCall.Arguments.GetArrayLength());
    }
}
