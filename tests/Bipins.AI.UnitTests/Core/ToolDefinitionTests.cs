using Bipins.AI.Core.Models;
using System.Text.Json;
using Xunit;

namespace Bipins.AI.UnitTests.Core;

public class ToolDefinitionTests
{
    [Fact]
    public void ToolDefinition_WithAllProperties_CreatesSuccessfully()
    {
        var parameters = JsonSerializer.SerializeToElement(new
        {
            type = "object",
            properties = new
            {
                location = new { type = "string", description = "City name" }
            },
            required = new[] { "location" }
        });

        var tool = new ToolDefinition("get_weather", "Get the current weather", parameters);

        Assert.Equal("get_weather", tool.Name);
        Assert.Equal("Get the current weather", tool.Description);
        Assert.True(tool.Parameters.ValueKind == JsonValueKind.Object);
        Assert.Equal("object", tool.Parameters.GetProperty("type").GetString());
    }

    [Fact]
    public void ToolDefinition_WithMinimalSchema_CreatesSuccessfully()
    {
        var parameters = JsonSerializer.SerializeToElement(new { type = "object" });
        var tool = new ToolDefinition("simple_tool", "Simple tool", parameters);

        Assert.Equal("simple_tool", tool.Name);
        Assert.Equal("Simple tool", tool.Description);
    }

    [Fact]
    public void ToolDefinition_WithComplexSchema_HandlesCorrectly()
    {
        var parameters = JsonSerializer.SerializeToElement(new
        {
            type = "object",
            properties = new
            {
                name = new { type = "string" },
                age = new { type = "number" },
                active = new { type = "boolean" }
            }
        });

        var tool = new ToolDefinition("complex_tool", "Complex tool", parameters);

        Assert.True(tool.Parameters.GetProperty("properties").ValueKind == JsonValueKind.Object);
    }
}
