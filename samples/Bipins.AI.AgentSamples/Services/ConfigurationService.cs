using Microsoft.Extensions.Configuration;

namespace Bipins.AI.AgentSamples.Services;

/// <summary>
/// Service for configuration validation.
/// Follows Single Responsibility Principle - only handles configuration validation.
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationService"/> class.
    /// </summary>
    public ConfigurationService(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <inheritdoc />
    public bool ValidateConfiguration(out string? errorMessage)
    {
        // Try configuration first (appsettings.json, user secrets)
        var configApiKey = _configuration.GetValue<string>("OpenAI:ApiKey");
        
        // If not in config or empty, try environment variable
        var apiKey = !string.IsNullOrWhiteSpace(configApiKey) 
            ? configApiKey 
            : Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            errorMessage = "OpenAI API key not configured. Set it in appsettings.json, user secrets, or OPENAI_API_KEY environment variable.";
            return false;
        }

        errorMessage = null;
        return true;
    }

    /// <inheritdoc />
    public bool IsVectorStoreConfigured()
    {
        var endpoint = _configuration.GetValue<string>("Qdrant:Endpoint")
                    ?? Environment.GetEnvironmentVariable("QDRANT_ENDPOINT");
        return !string.IsNullOrWhiteSpace(endpoint);
    }
}
