namespace Bipins.AI.Agents;

/// <summary>
/// Registry for managing registered agents.
/// </summary>
public interface IAgentRegistry
{
    /// <summary>
    /// Registers an agent.
    /// </summary>
    void RegisterAgent(IAgent agent);

    /// <summary>
    /// Gets an agent by ID.
    /// </summary>
    IAgent? GetAgent(string agentId);

    /// <summary>
    /// Gets all registered agents.
    /// </summary>
    IReadOnlyList<IAgent> GetAllAgents();
}
