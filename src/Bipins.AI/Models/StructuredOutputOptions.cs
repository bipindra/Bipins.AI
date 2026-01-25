using System.Text.Json;

namespace Bipins.AI.Core.Models;

/// <summary>
/// Options for structured output (JSON schema constraint).
/// </summary>
/// <param name="Schema">JSON schema that the output must conform to.</param>
/// <param name="ResponseFormat">Response format type (e.g., "json_object", "json_schema").</param>
public record StructuredOutputOptions(
    JsonElement Schema,
    string ResponseFormat = "json_schema");
