using Bipins.AI.Core.Models;
using System.Text.Json;
using Xunit;

namespace Bipins.AI.UnitTests.Core;

public class StructuredOutputHelperTests
{
    [Fact]
    public void ExtractStructuredOutput_WithValidJson_ReturnsJsonElement()
    {
        var json = "{\"name\": \"test\", \"value\": 123}";
        var result = StructuredOutputHelper.ExtractStructuredOutput(json);

        Assert.NotNull(result);
        Assert.True(result.HasValue);
        Assert.Equal(JsonValueKind.Object, result.Value.ValueKind);
        Assert.Equal("test", result.Value.GetProperty("name").GetString());
        Assert.Equal(123, result.Value.GetProperty("value").GetInt32());
    }

    [Fact]
    public void ExtractStructuredOutput_WithJsonArray_ReturnsJsonElement()
    {
        var json = "[{\"id\": 1}, {\"id\": 2}]";
        var result = StructuredOutputHelper.ExtractStructuredOutput(json);

        Assert.NotNull(result);
        Assert.True(result.HasValue);
        Assert.Equal(JsonValueKind.Array, result.Value.ValueKind);
        Assert.Equal(2, result.Value.GetArrayLength());
    }

    [Fact]
    public void ExtractStructuredOutput_WithJsonInText_ExtractsJson()
    {
        var text = "Here is the result: {\"status\": \"success\", \"code\": 200}";
        var result = StructuredOutputHelper.ExtractStructuredOutput(text);

        Assert.NotNull(result);
        Assert.True(result.HasValue);
        Assert.Equal("success", result.Value.GetProperty("status").GetString());
        Assert.Equal(200, result.Value.GetProperty("code").GetInt32());
    }

    [Fact]
    public void ExtractStructuredOutput_WithInvalidJson_ReturnsNull()
    {
        var text = "This is not valid JSON { invalid }";
        var result = StructuredOutputHelper.ExtractStructuredOutput(text);

        // Should return null for invalid JSON
        Assert.Null(result);
    }

    [Fact]
    public void ExtractStructuredOutput_WithEmptyString_ReturnsNull()
    {
        var result = StructuredOutputHelper.ExtractStructuredOutput("");

        Assert.Null(result);
    }

    [Fact]
    public void ExtractStructuredOutput_WithWhitespace_ReturnsNull()
    {
        var result = StructuredOutputHelper.ExtractStructuredOutput("   ");

        Assert.Null(result);
    }

    [Fact]
    public void ParseAndValidate_WithValidJsonAndSchema_ReturnsJsonElement()
    {
        var json = "{\"name\": \"test\", \"age\": 25}";
        var schema = JsonSerializer.SerializeToElement(new
        {
            type = "object",
            properties = new
            {
                name = new { type = "string" },
                age = new { type = "number" }
            }
        });

        var result = StructuredOutputHelper.ParseAndValidate(json, schema);

        Assert.NotNull(result);
        Assert.True(result.HasValue);
    }

    [Fact]
    public void ParseAndValidate_WithInvalidJson_ReturnsNull()
    {
        var json = "invalid json";
        var schema = JsonSerializer.SerializeToElement(new { type = "object" });

        var result = StructuredOutputHelper.ParseAndValidate(json, schema);

        Assert.Null(result);
    }

    [Fact]
    public void ParseAndValidate_WithNullSchema_ReturnsParsedJson()
    {
        var json = "{\"name\": \"test\"}";
        var nullSchema = JsonSerializer.SerializeToElement((object?)null);

        var result = StructuredOutputHelper.ParseAndValidate(json, nullSchema);

        // Should still parse even with null schema
        Assert.NotNull(result);
    }
}
