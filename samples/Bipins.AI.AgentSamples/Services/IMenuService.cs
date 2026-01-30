using Bipins.AI.AgentSamples.Core;

namespace Bipins.AI.AgentSamples.Services;

/// <summary>
/// Interface for interactive menu service.
/// Follows Interface Segregation Principle - focused interface for menu operations.
/// </summary>
public interface IMenuService
{
    /// <summary>
    /// Displays the menu and handles user interaction.
    /// </summary>
    Task ShowMenuAsync(IReadOnlyList<IScenario> scenarios, CancellationToken cancellationToken = default);
}
