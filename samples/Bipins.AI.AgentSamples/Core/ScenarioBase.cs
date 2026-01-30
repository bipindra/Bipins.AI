using Bipins.AI.Agents;
using Bipins.AI.AgentSamples.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Bipins.AI.AgentSamples.Core;

/// <summary>
/// Base class for scenarios providing common functionality.
/// Follows Open/Closed Principle - open for extension, closed for modification.
/// </summary>
public abstract class ScenarioBase : IScenario
{
    protected readonly IAgent Agent;
    protected readonly IOutputFormatter OutputFormatter;
    protected readonly ILogger Logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScenarioBase"/> class.
    /// </summary>
    protected ScenarioBase(
        IAgent agent,
        IOutputFormatter outputFormatter,
        ILogger logger)
    {
        Agent = agent ?? throw new ArgumentNullException(nameof(agent));
        OutputFormatter = outputFormatter ?? throw new ArgumentNullException(nameof(outputFormatter));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public abstract int Number { get; }

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public abstract string Description { get; }

    /// <inheritdoc />
    public virtual bool RequiresVectorStore => false;

    /// <inheritdoc />
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            OutputFormatter.WriteScenarioHeader(Number, Name);
            OutputFormatter.WriteGoal(GetGoal());
            OutputFormatter.WriteExecuting();

            await ExecuteScenarioAsync(stopwatch, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error executing scenario {Scenario}", Name);
            OutputFormatter.WriteError($"Error: {ex.Message}");
            throw;
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    /// <summary>
    /// Gets the goal description for this scenario.
    /// </summary>
    protected abstract string GetGoal();

    /// <summary>
    /// Executes the specific scenario logic.
    /// </summary>
    protected abstract Task ExecuteScenarioAsync(Stopwatch stopwatch, CancellationToken cancellationToken);
}
