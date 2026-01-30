namespace Bipins.AI.AgentSamples.Services;

/// <summary>
/// Interface for configuration service.
/// Follows Interface Segregation Principle - focused interface for configuration operations.
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Validates that required configuration is present.
    /// </summary>
    bool ValidateConfiguration(out string? errorMessage);

    /// <summary>
    /// Checks if vector store is configured.
    /// </summary>
    bool IsVectorStoreConfigured();
}
