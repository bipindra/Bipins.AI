using Microsoft.Extensions.Logging;

namespace Bipins.AI.Agents;

/// <summary>
/// Default implementation of agent registry.
/// </summary>
public class DefaultAgentRegistry : IAgentRegistry
{
    private readonly Dictionary<string, IAgent> _agents = new();
    private readonly ILogger<DefaultAgentRegistry>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultAgentRegistry"/> class.
    /// </summary>
    public DefaultAgentRegistry(ILogger<DefaultAgentRegistry>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public void RegisterAgent(IAgent agent)
    {
        if (string.IsNullOrWhiteSpace(agent.Id))
        {
            throw new ArgumentException("Agent ID cannot be null or empty.", nameof(agent));
        }

        _agents[agent.Id] = agent;
        _logger?.LogDebug("Registered agent: {AgentId} ({AgentName})", agent.Id, agent.Name);
    }

    /// <inheritdoc />
    public IAgent? GetAgent(string agentId)
    {
        _agents.TryGetValue(agentId, out var agent);
        return agent;
    }

    /// <inheritdoc />
    public IReadOnlyList<IAgent> GetAllAgents()
    {
        return _agents.Values.ToList();
    }
}
