using Bipins.AI.Core.Models;
using Xunit;

namespace Bipins.AI.UnitTests.Core;

public class MessageTests
{
    [Fact]
    public void Message_WithUserRole_CreatesSuccessfully()
    {
        var message = new Message(MessageRole.User, "Hello");

        Assert.Equal(MessageRole.User, message.Role);
        Assert.Equal("Hello", message.Content);
        Assert.Null(message.ToolCallId);
    }

    [Fact]
    public void Message_WithAssistantRole_CreatesSuccessfully()
    {
        var message = new Message(MessageRole.Assistant, "Hi there!");

        Assert.Equal(MessageRole.Assistant, message.Role);
        Assert.Equal("Hi there!", message.Content);
    }

    [Fact]
    public void Message_WithSystemRole_CreatesSuccessfully()
    {
        var message = new Message(MessageRole.System, "You are a helpful assistant");

        Assert.Equal(MessageRole.System, message.Role);
        Assert.Equal("You are a helpful assistant", message.Content);
    }

    [Fact]
    public void Message_WithToolCallId_IncludesToolCallId()
    {
        var message = new Message(MessageRole.Tool, "Result", "call_123");

        Assert.Equal(MessageRole.Tool, message.Role);
        Assert.Equal("Result", message.Content);
        Assert.Equal("call_123", message.ToolCallId);
    }

    [Fact]
    public void Message_WithEmptyContent_HandlesCorrectly()
    {
        var message = new Message(MessageRole.User, "");

        Assert.Equal("", message.Content);
    }
}
