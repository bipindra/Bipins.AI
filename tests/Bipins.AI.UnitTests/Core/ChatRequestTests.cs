using Bipins.AI.Core.Models;
using System.Text.Json;
using Xunit;

namespace Bipins.AI.UnitTests.Core;

public class ChatRequestTests
{
    [Fact]
    public void ChatRequest_WithMessages_CreatesSuccessfully()
    {
        var messages = new[]
        {
            new Message(MessageRole.User, "Hello"),
            new Message(MessageRole.Assistant, "Hi there!")
        };

        var request = new ChatRequest(messages);

        Assert.Equal(2, request.Messages.Count);
        Assert.Equal(MessageRole.User, request.Messages[0].Role);
        Assert.Equal("Hello", request.Messages[0].Content);
    }

    [Fact]
    public void ChatRequest_WithTools_IncludesTools()
    {
        var messages = new[] { new Message(MessageRole.User, "What's the weather?") };
        var tools = new List<ToolDefinition>
        {
            new ToolDefinition("get_weather", "Get weather", JsonSerializer.SerializeToElement(new { type = "object" }))
        };

        var request = new ChatRequest(messages, Tools: tools);

        Assert.NotNull(request.Tools);
        Assert.Single(request.Tools);
        Assert.Equal("get_weather", request.Tools[0].Name);
    }

    [Fact]
    public void ChatRequest_WithStructuredOutput_IncludesStructuredOutput()
    {
        var messages = new[] { new Message(MessageRole.User, "Extract data") };
        var schema = JsonSerializer.SerializeToElement(new
        {
            type = "object",
            properties = new { name = new { type = "string" } }
        });
        var structuredOutput = new StructuredOutputOptions(schema, "json_schema");

        var request = new ChatRequest(messages, StructuredOutput: structuredOutput);

        Assert.NotNull(request.StructuredOutput);
        Assert.Equal("json_schema", request.StructuredOutput.ResponseFormat);
    }

    [Fact]
    public void ChatRequest_WithTemperature_SetsTemperature()
    {
        var messages = new[] { new Message(MessageRole.User, "Hello") };
        var request = new ChatRequest(messages, Temperature: 0.7f);

        Assert.Equal(0.7f, request.Temperature);
    }

    [Fact]
    public void ChatRequest_WithMaxTokens_SetsMaxTokens()
    {
        var messages = new[] { new Message(MessageRole.User, "Hello") };
        var request = new ChatRequest(messages, MaxTokens: 500);

        Assert.Equal(500, request.MaxTokens);
    }

    [Fact]
    public void ChatRequest_WithMetadata_IncludesMetadata()
    {
        var messages = new[] { new Message(MessageRole.User, "Hello") };
        var metadata = new Dictionary<string, object> { { "modelId", "gpt-4" } };
        var request = new ChatRequest(messages, Metadata: metadata);

        Assert.NotNull(request.Metadata);
        Assert.Equal("gpt-4", request.Metadata["modelId"]);
    }
}
