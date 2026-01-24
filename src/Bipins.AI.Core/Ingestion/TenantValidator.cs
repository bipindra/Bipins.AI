using System.Text.RegularExpressions;

namespace Bipins.AI.Core.Ingestion;

/// <summary>
/// Validates tenant identifiers.
/// </summary>
public static class TenantValidator
{
    private static readonly Regex TenantIdRegex = new(@"^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);

    /// <summary>
    /// Validates a tenant ID format.
    /// </summary>
    /// <param name="tenantId">The tenant ID to validate.</param>
    /// <returns>True if valid, false otherwise.</returns>
    public static bool IsValid(string? tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return false;
        }

        if (tenantId.Length > 100)
        {
            return false;
        }

        return TenantIdRegex.IsMatch(tenantId);
    }

    /// <summary>
    /// Validates and throws an exception if invalid.
    /// </summary>
    /// <param name="tenantId">The tenant ID to validate.</param>
    /// <exception cref="ArgumentException">Thrown if tenant ID is invalid.</exception>
    public static void ValidateOrThrow(string? tenantId)
    {
        if (!IsValid(tenantId))
        {
            throw new ArgumentException($"Invalid tenant ID format: {tenantId}", nameof(tenantId));
        }
    }
}
