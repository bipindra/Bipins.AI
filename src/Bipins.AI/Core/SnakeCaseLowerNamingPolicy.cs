using System.Text.Json;

namespace Bipins.AI.Core;

/// <summary>
/// Custom JSON naming policy for snake_case_lower (shared across providers, compatible with .NET 7).
/// This is useful for serializing objects to snake_case JSON format, which is commonly used by AI APIs.
/// </summary>
public sealed class SnakeCaseLowerNamingPolicy : JsonNamingPolicy
{
    public static SnakeCaseLowerNamingPolicy Instance { get; } = new SnakeCaseLowerNamingPolicy();

    public override string ConvertName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var result = new System.Text.StringBuilder();

        for (int i = 0; i < name.Length; i++)
        {
            var c = name[i];

            if (char.IsUpper(c))
            {
                // Add underscore before uppercase if:
                // 1. Not the first character
                // 2. Either previous char was lowercase, or next char is lowercase (handles acronyms like "URLValue")
                if (i > 0)
                {
                    var prevChar = name[i - 1];
                    var nextChar = i < name.Length - 1 ? name[i + 1] : (char?)null;
                    
                    // Add underscore if previous was lowercase, or if we're in an acronym followed by lowercase
                    if (char.IsLower(prevChar) || (nextChar.HasValue && char.IsLower(nextChar.Value)))
                    {
                        result.Append('_');
                    }
                }

                result.Append(char.ToLowerInvariant(c));
            }
            else
            {
                result.Append(c);
            }
        }

        return result.ToString();
    }
}

