using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bipins.AI.Providers.OpenAI.Models;

/// <summary>
/// OpenAI response_format for structured output (snake_case for API).
/// </summary>
internal sealed class OpenAiResponseFormat
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "json_schema";

    [JsonPropertyName("json_schema")]
    public OpenAiJsonSchemaFormat? JsonSchema { get; set; }
}

/// <summary>
/// OpenAI json_schema wrapper (name, schema, strict).
/// </summary>
internal sealed class OpenAiJsonSchemaFormat
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "response_schema";

    [JsonPropertyName("schema")]
    public JsonElement Schema { get; set; }

    [JsonPropertyName("strict")]
    public bool Strict { get; set; } = true;
}
