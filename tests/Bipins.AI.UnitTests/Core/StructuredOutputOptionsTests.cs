using Bipins.AI.Core.Models;
using System.Text.Json;
using Xunit;

namespace Bipins.AI.UnitTests.Core;

public class StructuredOutputOptionsTests
{
    [Fact]
    public void StructuredOutputOptions_WithSchema_CreatesSuccessfully()
    {
        var schema = JsonSerializer.SerializeToElement(new
        {
            type = "object",
            properties = new { name = new { type = "string" } }
        });

        var options = new StructuredOutputOptions(schema, "json_schema");

        Assert.Equal("json_schema", options.ResponseFormat);
        Assert.True(options.Schema.ValueKind == JsonValueKind.Object);
    }

    [Fact]
    public void StructuredOutputOptions_WithJsonObjectFormat_CreatesSuccessfully()
    {
        var schema = JsonSerializer.SerializeToElement(new { type = "object" });
        var options = new StructuredOutputOptions(schema, "json_object");

        Assert.Equal("json_object", options.ResponseFormat);
    }
}
