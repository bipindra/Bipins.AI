using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bipins.AI.Connectors.Llm.OpenAI.Models;

/// <summary>
/// Internal DTO for OpenAI tool call.
/// </summary>
internal record OpenAiToolCall(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("function")] OpenAiFunctionCall Function);

/// <summary>
/// Internal DTO for OpenAI function call.
/// </summary>
internal record OpenAiFunctionCall(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("arguments")] string Arguments);
