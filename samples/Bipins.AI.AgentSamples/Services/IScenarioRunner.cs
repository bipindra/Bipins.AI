using Bipins.AI.AgentSamples.Core;

namespace Bipins.AI.AgentSamples.Services;

/// <summary>
/// Interface for scenario runner service.
/// Follows Interface Segregation Principle - focused interface for scenario execution.
/// </summary>
public interface IScenarioRunner
{
    /// <summary>
    /// Runs all available scenarios.
    /// </summary>
    Task RunAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a specific scenario by number.
    /// </summary>
    Task RunScenarioAsync(int scenarioNumber, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for running scenarios.
/// Follows Single Responsibility Principle - only handles scenario execution orchestration.
/// </summary>
public class ScenarioRunner : IScenarioRunner
{
    private readonly IReadOnlyList<IScenario> _scenarios;
    private readonly IOutputFormatter _outputFormatter;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScenarioRunner"/> class.
    /// </summary>
    public ScenarioRunner(
        IEnumerable<IScenario> scenarios,
        IOutputFormatter outputFormatter)
    {
        _scenarios = scenarios?.ToList() ?? throw new ArgumentNullException(nameof(scenarios));
        _outputFormatter = outputFormatter ?? throw new ArgumentNullException(nameof(outputFormatter));
    }

    /// <inheritdoc />
    public async Task RunAllAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("🚀 Running all scenarios...");
        Console.WriteLine();

        foreach (var scenario in _scenarios.OrderBy(s => s.Number))
        {
            try
            {
                await scenario.ExecuteAsync(cancellationToken);
                await Task.Delay(500, cancellationToken); // Brief pause between scenarios
            }
            catch (Exception ex)
            {
                _outputFormatter.WriteError($"Error in scenario {scenario.Number}: {ex.Message}");
            }
        }
    }

    /// <inheritdoc />
    public async Task RunScenarioAsync(int scenarioNumber, CancellationToken cancellationToken = default)
    {
        var scenario = _scenarios.FirstOrDefault(s => s.Number == scenarioNumber);
        if (scenario == null)
        {
            throw new ArgumentException($"Scenario {scenarioNumber} not found.", nameof(scenarioNumber));
        }

        await scenario.ExecuteAsync(cancellationToken);
    }
}
