using Microsoft.Extensions.Configuration;

namespace Bipins.AI.Core.Configuration;

/// <summary>
/// Extension methods for reading configuration values from multiple sources.
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Gets a configuration value from configuration (including user secrets) or environment variables.
    /// Priority: Configuration (appsettings.json, user secrets) > Environment Variables.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="configKey">The configuration key (e.g., "OpenAI:ApiKey").</param>
    /// <param name="envVarName">The environment variable name (e.g., "OPENAI_API_KEY"). If null, will be derived from configKey.</param>
    /// <returns>The configuration value, or null if not found.</returns>
    public static string? GetValueOrEnvironmentVariable(
        this IConfiguration configuration,
        string configKey,
        string? envVarName = null)
    {
        // First, try configuration (includes appsettings.json and user secrets)
        var value = configuration[configKey];
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        // Fall back to environment variable
        envVarName ??= DeriveEnvironmentVariableName(configKey);
        return Environment.GetEnvironmentVariable(envVarName);
    }

    /// <summary>
    /// Gets a configuration value from configuration (including user secrets) or environment variables.
    /// Throws an exception if the value is not found.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="configKey">The configuration key (e.g., "OpenAI:ApiKey").</param>
    /// <param name="envVarName">The environment variable name (e.g., "OPENAI_API_KEY"). If null, will be derived from configKey.</param>
    /// <param name="errorMessage">Custom error message if value is not found.</param>
    /// <returns>The configuration value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the value is not found.</exception>
    public static string GetRequiredValueOrEnvironmentVariable(
        this IConfiguration configuration,
        string configKey,
        string? envVarName = null,
        string? errorMessage = null)
    {
        var value = configuration.GetValueOrEnvironmentVariable(configKey, envVarName);
        if (string.IsNullOrWhiteSpace(value))
        {
            var message = errorMessage ?? $"{configKey} not configured. Set it in appsettings.json, user secrets, or environment variable {envVarName ?? DeriveEnvironmentVariableName(configKey)}";
            throw new InvalidOperationException(message);
        }

        return value;
    }

    /// <summary>
    /// Derives an environment variable name from a configuration key.
    /// Example: "OpenAI:ApiKey" -> "OPENAI_API_KEY"
    /// </summary>
    private static string DeriveEnvironmentVariableName(string configKey)
    {
        return configKey
            .Replace(":", "_")
            .ToUpperInvariant();
    }
}
