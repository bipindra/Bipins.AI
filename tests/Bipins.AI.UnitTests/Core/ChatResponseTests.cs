using Bipins.AI.Core.Models;
using System.Text.Json;
using Xunit;

namespace Bipins.AI.UnitTests.Core;

public class ChatResponseTests
{
    [Fact]
    public void ChatResponse_WithAllProperties_CreatesSuccessfully()
    {
        var usage = new Usage(100, 50, 150);
        var response = new ChatResponse(
            "Test response",
            null,
            usage,
            "gpt-4",
            "stop",
            null,
            null);

        Assert.Equal("Test response", response.Content);
        Assert.Null(response.ToolCalls);
        Assert.NotNull(response.Usage);
        Assert.Equal(100, response.Usage.PromptTokens);
        Assert.Equal(50, response.Usage.CompletionTokens);
        Assert.Equal(150, response.Usage.TotalTokens);
        Assert.Equal("gpt-4", response.ModelId);
        Assert.Equal("stop", response.FinishReason);
    }

    [Fact]
    public void ChatResponse_WithToolCalls_IncludesToolCalls()
    {
        var toolCalls = new List<ToolCall>
        {
            new ToolCall("call_1", "get_weather", JsonSerializer.SerializeToElement(new { location = "Seattle" }))
        };

        var response = new ChatResponse(
            "Response",
            toolCalls,
            null,
            "gpt-4",
            "tool_calls");

        Assert.NotNull(response.ToolCalls);
        Assert.Single(response.ToolCalls);
        Assert.Equal("call_1", response.ToolCalls[0].Id);
        Assert.Equal("get_weather", response.ToolCalls[0].Name);
    }

    [Fact]
    public void ChatResponse_WithStructuredOutput_IncludesStructuredOutput()
    {
        var structuredOutput = JsonSerializer.SerializeToElement(new { name = "test", value = 123 });

        var response = new ChatResponse(
            "Response",
            null,
            null,
            "gpt-4",
            "stop",
            null,
            structuredOutput);

        Assert.True(response.StructuredOutput.HasValue);
        Assert.Equal("test", response.StructuredOutput.Value.GetProperty("name").GetString());
        Assert.Equal(123, response.StructuredOutput.Value.GetProperty("value").GetInt32());
    }

    [Fact]
    public void ChatResponse_WithMinimalProperties_CreatesSuccessfully()
    {
        var response = new ChatResponse("Minimal response");

        Assert.Equal("Minimal response", response.Content);
        Assert.Null(response.ToolCalls);
        Assert.Null(response.Usage);
        Assert.Null(response.ModelId);
        Assert.Null(response.FinishReason);
    }
}
