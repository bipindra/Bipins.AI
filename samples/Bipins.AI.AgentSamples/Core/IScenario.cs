namespace Bipins.AI.AgentSamples.Core;

/// <summary>
/// Interface for agent demonstration scenarios.
/// Follows Interface Segregation Principle - focused interface for scenarios.
/// </summary>
public interface IScenario
{
    /// <summary>
    /// Gets the scenario number.
    /// </summary>
    int Number { get; }

    /// <summary>
    /// Gets the scenario name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the scenario description.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets whether this scenario requires vector store.
    /// </summary>
    bool RequiresVectorStore { get; }

    /// <summary>
    /// Executes the scenario.
    /// </summary>
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
