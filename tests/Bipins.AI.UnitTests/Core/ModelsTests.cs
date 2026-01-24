using Bipins.AI.Core.Models;
using System.Text.Json;
using Xunit;

namespace Bipins.AI.UnitTests.Core;

public class ModelsTests
{
    [Fact]
    public void Message_Creation_SetsProperties()
    {
        var message = new Message(MessageRole.User, "Hello", "tool-call-1");

        Assert.Equal(MessageRole.User, message.Role);
        Assert.Equal("Hello", message.Content);
        Assert.Equal("tool-call-1", message.ToolCallId);
    }

    [Fact]
    public void Message_WithoutToolCallId_CreatesMessage()
    {
        var message = new Message(MessageRole.Assistant, "Hi there");

        Assert.Equal(MessageRole.Assistant, message.Role);
        Assert.Equal("Hi there", message.Content);
        Assert.Null(message.ToolCallId);
    }

    [Fact]
    public void ChatRequest_Creation_SetsProperties()
    {
        var messages = new[] { new Message(MessageRole.User, "Hello") };
        var tools = new[] { new ToolDefinition("function1", "description", JsonDocument.Parse("{}").RootElement) };
        var metadata = new Dictionary<string, object> { { "key", "value" } };
        var structuredOutput = new StructuredOutputOptions(JsonDocument.Parse("{\"type\":\"object\"}").RootElement, "json_schema");

        var request = new ChatRequest(
            Messages: messages,
            Tools: tools,
            ToolChoice: "auto",
            Temperature: 0.7f,
            MaxTokens: 100,
            Metadata: metadata,
            StructuredOutput: structuredOutput);

        Assert.Equal(messages, request.Messages);
        Assert.Equal(tools, request.Tools);
        Assert.Equal("auto", request.ToolChoice);
        Assert.Equal(0.7f, request.Temperature);
        Assert.Equal(100, request.MaxTokens);
        Assert.Equal(metadata, request.Metadata);
        Assert.Equal(structuredOutput, request.StructuredOutput);
    }

    [Fact]
    public void ChatRequest_Minimal_CreatesRequest()
    {
        var messages = new[] { new Message(MessageRole.User, "Hello") };
        var request = new ChatRequest(messages);

        Assert.Equal(messages, request.Messages);
        Assert.Null(request.Tools);
        Assert.Null(request.ToolChoice);
        Assert.Null(request.Temperature);
        Assert.Null(request.MaxTokens);
        Assert.Null(request.Metadata);
        Assert.Null(request.StructuredOutput);
    }

    [Fact]
    public void ChatResponse_Creation_SetsProperties()
    {
        var toolCalls = new[] { new ToolCall("call1", "function1", JsonDocument.Parse("{}").RootElement) };
        var usage = new Usage(10, 20, 30);
        var safety = new SafetyInfo(false, null);
        var structuredOutput = JsonDocument.Parse("{\"key\":\"value\"}").RootElement;

        var response = new ChatResponse(
            Content: "Response text",
            ToolCalls: toolCalls,
            Usage: usage,
            ModelId: "gpt-4",
            FinishReason: "stop",
            Safety: safety,
            StructuredOutput: structuredOutput);

        Assert.Equal("Response text", response.Content);
        Assert.Equal(toolCalls, response.ToolCalls);
        Assert.Equal(usage, response.Usage);
        Assert.Equal("gpt-4", response.ModelId);
        Assert.Equal("stop", response.FinishReason);
        Assert.Equal(safety, response.Safety);
        Assert.Equal(structuredOutput, response.StructuredOutput);
    }

    [Fact]
    public void ChatResponse_Minimal_CreatesResponse()
    {
        var response = new ChatResponse("Response text");

        Assert.Equal("Response text", response.Content);
        Assert.Null(response.ToolCalls);
        Assert.Null(response.Usage);
        Assert.Null(response.ModelId);
        Assert.Null(response.FinishReason);
        Assert.Null(response.Safety);
        Assert.Null(response.StructuredOutput);
    }

    [Fact]
    public void EmbeddingRequest_Creation_SetsProperties()
    {
        var inputs = new[] { "text1", "text2" };
        var metadata = new Dictionary<string, object> { { "key", "value" } };

        var request = new EmbeddingRequest(
            Inputs: inputs,
            ModelId: "text-embedding-ada-002",
            Metadata: metadata);

        Assert.Equal(inputs, request.Inputs);
        Assert.Equal("text-embedding-ada-002", request.ModelId);
        Assert.Equal(metadata, request.Metadata);
    }

    [Fact]
    public void EmbeddingRequest_Minimal_CreatesRequest()
    {
        var inputs = new[] { "text1" };
        var request = new EmbeddingRequest(inputs);

        Assert.Equal(inputs, request.Inputs);
        Assert.Null(request.ModelId);
        Assert.Null(request.Metadata);
    }

    [Fact]
    public void EmbeddingResponse_Creation_SetsProperties()
    {
        var vectors = new[]
        {
            new ReadOnlyMemory<float>(new float[] { 0.1f, 0.2f }),
            new ReadOnlyMemory<float>(new float[] { 0.3f, 0.4f })
        };
        var usage = new Usage(5, 0, 5);

        var response = new EmbeddingResponse(
            Vectors: vectors,
            Usage: usage,
            ModelId: "text-embedding-ada-002");

        Assert.Equal(vectors, response.Vectors);
        Assert.Equal(usage, response.Usage);
        Assert.Equal("text-embedding-ada-002", response.ModelId);
    }

    [Fact]
    public void EmbeddingResponse_Minimal_CreatesResponse()
    {
        var vectors = new[] { new ReadOnlyMemory<float>(new float[] { 0.1f }) };
        var response = new EmbeddingResponse(vectors);

        Assert.Equal(vectors, response.Vectors);
        Assert.Null(response.Usage);
        Assert.Null(response.ModelId);
    }

    [Fact]
    public void Usage_Creation_SetsProperties()
    {
        var usage = new Usage(10, 20, 30);

        Assert.Equal(10, usage.PromptTokens);
        Assert.Equal(20, usage.CompletionTokens);
        Assert.Equal(30, usage.TotalTokens);
    }

    [Fact]
    public void Usage_TotalTokens_MatchesSum()
    {
        var usage = new Usage(10, 20, 30);

        Assert.Equal(usage.PromptTokens + usage.CompletionTokens, usage.TotalTokens);
    }

    [Fact]
    public void Usage_ZeroTokens_CreatesUsage()
    {
        var usage = new Usage(0, 0, 0);

        Assert.Equal(0, usage.PromptTokens);
        Assert.Equal(0, usage.CompletionTokens);
        Assert.Equal(0, usage.TotalTokens);
    }
}
