using Microsoft.OpenApi.Models;

namespace Bipins.AI.Agents.Tools.CodeGen;

/// <summary>
/// Maps OpenAPI schema types to C# types.
/// </summary>
public static class TypeMapper
{
    /// <summary>
    /// Maps an OpenAPI schema to a C# type string.
    /// </summary>
    /// <param name="schema">The OpenAPI schema to map.</param>
    /// <param name="useNullable">Whether to use nullable reference types.</param>
    /// <returns>C# type string (e.g., "string", "int?", "List&lt;User&gt;").</returns>
    public static string MapToCSharpType(OpenApiSchema schema, bool useNullable = true)
    {
        if (schema == null)
            return "object";

        // Handle references first
        if (schema.Reference != null)
        {
            var typeName = GetTypeNameFromReference(schema.Reference.Id);
            return schema.Nullable && useNullable ? $"{typeName}?" : typeName;
        }

        // Handle arrays
        if (schema.Type == "array" && schema.Items != null)
        {
            var itemType = MapToCSharpType(schema.Items, useNullable);
            var listType = $"List<{itemType}>";
            return schema.Nullable && useNullable ? $"{listType}?" : listType;
        }

        // Handle objects
        if (schema.Type == "object" || schema.Properties?.Count > 0)
        {
            // If it has properties, it should be a custom class
            // This will be generated separately
            if (schema.Properties?.Count > 0)
            {
                return "object"; // Placeholder - will be a custom type
            }

            // Dictionary for generic objects
            var dictType = "Dictionary<string, object>";
            return schema.Nullable && useNullable ? $"{dictType}?" : dictType;
        }

        // Handle primitive types
        var baseType = MapPrimitiveType(schema.Type, schema.Format);
        
        // Apply nullability
        if (schema.Nullable && useNullable)
        {
            return IsValueType(baseType) ? $"{baseType}?" : $"{baseType}?";
        }

        return baseType;
    }

    /// <summary>
    /// Maps OpenAPI primitive types to C# types.
    /// </summary>
    private static string MapPrimitiveType(string type, string? format)
    {
        return type?.ToLowerInvariant() switch
        {
            "string" => MapStringType(format),
            "integer" => MapIntegerType(format),
            "number" => MapNumberType(format),
            "boolean" => "bool",
            _ => "object"
        };
    }

    /// <summary>
    /// Maps OpenAPI string types based on format.
    /// </summary>
    private static string MapStringType(string? format)
    {
        return format?.ToLowerInvariant() switch
        {
            "date-time" => "DateTime",
            "date" => "DateOnly",
            "time" => "TimeOnly",
            "uuid" => "Guid",
            "byte" => "byte[]",
            "binary" => "Stream",
            "password" => "string",
            "email" => "string",
            "uri" => "string",
            "hostname" => "string",
            "ipv4" => "string",
            "ipv6" => "string",
            _ => "string"
        };
    }

    /// <summary>
    /// Maps OpenAPI integer types based on format.
    /// </summary>
    private static string MapIntegerType(string? format)
    {
        return format?.ToLowerInvariant() switch
        {
            "int64" => "long",
            "int32" => "int",
            _ => "int" // Default to int
        };
    }

    /// <summary>
    /// Maps OpenAPI number types based on format.
    /// </summary>
    private static string MapNumberType(string? format)
    {
        return format?.ToLowerInvariant() switch
        {
            "float" => "float",
            "double" => "double",
            "decimal" => "decimal",
            _ => "double" // Default to double
        };
    }

    /// <summary>
    /// Determines if a C# type is a value type.
    /// </summary>
    private static bool IsValueType(string csharpType)
    {
        return csharpType switch
        {
            "int" or "long" or "short" or "byte" or "sbyte" or
            "uint" or "ulong" or "ushort" or
            "float" or "double" or "decimal" or
            "bool" or "char" or
            "DateTime" or "DateOnly" or "TimeOnly" or "Guid" or "TimeSpan" => true,
            _ => false
        };
    }

    /// <summary>
    /// Extracts a clean type name from an OpenAPI reference ID.
    /// </summary>
    /// <param name="referenceId">Reference ID (e.g., "#/components/schemas/User").</param>
    /// <returns>Type name (e.g., "User").</returns>
    public static string GetTypeNameFromReference(string referenceId)
    {
        // Reference IDs are typically like "#/components/schemas/User"
        // We want just "User"
        var parts = referenceId.Split('/');
        return parts[^1]; // Last part
    }

    /// <summary>
    /// Converts a name to PascalCase.
    /// </summary>
    public static string ToPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        // Handle snake_case, kebab-case, and camelCase
        var words = System.Text.RegularExpressions.Regex
            .Split(name, @"[_\-\s]+")
            .Select(w => w.Length > 0 ? char.ToUpperInvariant(w[0]) + w.Substring(1) : w);

        var result = string.Concat(words);

        // Ensure first character is uppercase
        if (result.Length > 0 && char.IsLower(result[0]))
        {
            result = char.ToUpperInvariant(result[0]) + result.Substring(1);
        }

        return result;
    }

    /// <summary>
    /// Converts a name to camelCase.
    /// </summary>
    public static string ToCamelCase(string name)
    {
        var pascal = ToPascalCase(name);
        if (string.IsNullOrEmpty(pascal))
            return pascal;

        return char.ToLowerInvariant(pascal[0]) + pascal.Substring(1);
    }
}
