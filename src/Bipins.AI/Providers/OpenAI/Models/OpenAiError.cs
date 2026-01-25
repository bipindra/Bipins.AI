using System.Text.Json.Serialization;

namespace Bipins.AI.Providers.OpenAI.Models;

/// <summary>
/// Internal DTO for OpenAI error.
/// </summary>
internal record OpenAiError(
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("type")] string? Type,
    [property: JsonPropertyName("code")] string? Code);

