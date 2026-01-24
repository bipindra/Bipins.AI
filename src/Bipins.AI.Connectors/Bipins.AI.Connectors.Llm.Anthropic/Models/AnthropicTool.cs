using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bipins.AI.Connectors.Llm.Anthropic.Models;

/// <summary>
/// Internal DTO for Anthropic tool definition.
/// </summary>
internal record AnthropicTool(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("input_schema")] JsonElement InputSchema);
