using System.Text.Json.Serialization;

namespace Bipins.AI.Connectors.Llm.OpenAI.Models;

/// <summary>
/// Internal DTO for OpenAI error response.
/// </summary>
internal record OpenAiErrorResponse(
    [property: JsonPropertyName("error")] OpenAiError? Error);
