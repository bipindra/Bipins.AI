using Bipins.AI.Core.Models;

namespace Bipins.AI.Agents;

/// <summary>
/// Core interface for AI agents that can execute tasks using tools and planning.
/// </summary>
public interface IAgent
{
    /// <summary>
    /// Unique identifier for the agent.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Human-readable name of the agent.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Capabilities of the agent.
    /// </summary>
    AgentCapabilities Capabilities { get; }

    /// <summary>
    /// Executes an agent request and returns a response.
    /// </summary>
    Task<AgentResponse> ExecuteAsync(AgentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an agent request and streams responses.
    /// </summary>
    IAsyncEnumerable<AgentResponseChunk> ExecuteStreamAsync(AgentRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Agent capabilities flags.
/// </summary>
[Flags]
public enum AgentCapabilities
{
    None = 0,
    ToolExecution = 1,
    Planning = 2,
    Memory = 4,
    Streaming = 8,
    All = ToolExecution | Planning | Memory | Streaming
}
