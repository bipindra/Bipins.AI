using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bipins.AI.Connectors.Llm.OpenAI.Models;

/// <summary>
/// Internal DTO for OpenAI function.
/// </summary>
internal record OpenAiFunction(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("parameters")] JsonElement Parameters);
