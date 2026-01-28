using System.Text.Json;

namespace Bipins.AI.Core;

/// <summary>
/// Custom JSON naming policy for snake_case_lower (shared across providers, compatible with .NET 7).
/// </summary>
internal sealed class SnakeCaseLowerNamingPolicy : JsonNamingPolicy
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
                if (i > 0)
                {
                    result.Append('_');
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

