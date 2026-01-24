using System.Text.Json;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;

namespace Bipins.AI.Core.Models;

/// <summary>
/// Helper class for parsing and validating structured output.
/// </summary>
public static class StructuredOutputHelper
{
    /// <summary>
    /// Parses structured JSON from a response string and validates it against a schema.
    /// </summary>
    /// <param name="responseContent">The response content to parse.</param>
    /// <param name="schema">The JSON schema to validate against.</param>
    /// <returns>Parsed JSON element if valid, null if invalid.</returns>
    public static JsonElement? ParseAndValidate(string responseContent, JsonElement schema)
    {
        try
        {
            // Try to parse the response as JSON
            var jsonDocument = JsonDocument.Parse(responseContent);
            var rootElement = jsonDocument.RootElement;

            // Basic validation - check if it's valid JSON
            if (rootElement.ValueKind == JsonValueKind.Undefined || rootElement.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            // If schema is provided, validate against it
            if (schema.ValueKind != JsonValueKind.Undefined && schema.ValueKind != JsonValueKind.Null)
            {
                // Note: Full JSON Schema validation would require a library like NJsonSchema
                // For now, we do basic structure validation
                if (!ValidateBasicStructure(rootElement, schema))
                {
                    return null;
                }
            }

            return rootElement;
        }
        catch (JsonException)
        {
            // Invalid JSON
            return null;
        }
    }

    /// <summary>
    /// Extracts structured output from a response, handling both JSON objects and JSON strings.
    /// </summary>
    /// <param name="responseContent">The response content.</param>
    /// <returns>Parsed JSON element if found, null otherwise.</returns>
    public static JsonElement? ExtractStructuredOutput(string responseContent)
    {
        if (string.IsNullOrWhiteSpace(responseContent))
        {
            return null;
        }

        // Try to parse as direct JSON
        try
        {
            var jsonDocument = JsonDocument.Parse(responseContent);
            return jsonDocument.RootElement;
        }
        catch (JsonException)
        {
            // Not valid JSON, might be a JSON string within text
            // Try to extract JSON object/array from the content
            var jsonStart = responseContent.IndexOf('{');
            var jsonArrayStart = responseContent.IndexOf('[');
            
            int startIndex = -1;
            if (jsonStart >= 0 && (jsonArrayStart < 0 || jsonStart < jsonArrayStart))
            {
                startIndex = jsonStart;
            }
            else if (jsonArrayStart >= 0)
            {
                startIndex = jsonArrayStart;
            }

            if (startIndex >= 0)
            {
                // Find matching closing brace/bracket
                var openChar = responseContent[startIndex];
                var closeChar = openChar == '{' ? '}' : ']';
                var depth = 0;
                var endIndex = startIndex;

                for (int i = startIndex; i < responseContent.Length; i++)
                {
                    if (responseContent[i] == openChar) depth++;
                    if (responseContent[i] == closeChar) depth--;
                    if (depth == 0)
                    {
                        endIndex = i;
                        break;
                    }
                }

                if (endIndex > startIndex)
                {
                    var jsonSubstring = responseContent.Substring(startIndex, endIndex - startIndex + 1);
                    try
                    {
                        var jsonDocument = JsonDocument.Parse(jsonSubstring);
                        return jsonDocument.RootElement;
                    }
                    catch (JsonException)
                    {
                        // Invalid JSON substring
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Validates basic structure of JSON element against schema.
    /// This is a simplified validator - full validation would require a JSON Schema library.
    /// </summary>
    private static bool ValidateBasicStructure(JsonElement element, JsonElement schema)
    {
        // Basic validation - check if schema has "type" property
        if (schema.TryGetProperty("type", out var typeProperty))
        {
            var expectedType = typeProperty.GetString();
            return expectedType switch
            {
                "object" => element.ValueKind == JsonValueKind.Object,
                "array" => element.ValueKind == JsonValueKind.Array,
                "string" => element.ValueKind == JsonValueKind.String,
                "number" => element.ValueKind == JsonValueKind.Number,
                "integer" => element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out _),
                "boolean" => element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False,
                "null" => element.ValueKind == JsonValueKind.Null,
                _ => true // Unknown type, allow it
            };
        }

        // If no type specified, accept any valid JSON
        return true;
    }
}
