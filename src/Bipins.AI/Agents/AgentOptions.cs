namespace Bipins.AI.Agents;

/// <summary>
/// Configuration options for an agent.
/// </summary>
public class AgentOptions
{
    /// <summary>
    /// Human-readable name of the agent.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// System prompt that defines the agent's behavior and capabilities.
    /// </summary>
    public string SystemPrompt { get; set; } = "You are a helpful AI assistant.";

    /// <summary>
    /// Optional model ID to use. If not specified, uses the default model from the LLM provider.
    /// </summary>
    public string? ModelId { get; set; }

    /// <summary>
    /// Temperature for generation (0-2).
    /// </summary>
    public float Temperature { get; set; } = 0.7f;

    /// <summary>
    /// Maximum number of iterations the agent can perform.
    /// </summary>
    public int MaxIterations { get; set; } = 10;

    /// <summary>
    /// Optional timeout for agent execution.
    /// </summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>
    /// Whether to enable planning before execution.
    /// </summary>
    public bool EnablePlanning { get; set; } = true;

    /// <summary>
    /// Whether to enable memory for conversation context.
    /// </summary>
    public bool EnableMemory { get; set; } = true;

    /// <summary>
    /// Memory configuration options.
    /// </summary>
    public AgentMemoryOptions? MemoryOptions { get; set; }

    /// <summary>
    /// Maximum tokens per request.
    /// </summary>
    public int? MaxTokens { get; set; }
}

/// <summary>
/// Options for agent memory.
/// </summary>
public class AgentMemoryOptions
{
    /// <summary>
    /// Maximum number of conversation turns to keep in memory.
    /// </summary>
    public int MaxConversationTurns { get; set; } = 50;

    /// <summary>
    /// Whether to enable semantic search in memory.
    /// </summary>
    public bool EnableSemanticSearch { get; set; } = true;

    /// <summary>
    /// Number of relevant memories to retrieve when searching.
    /// </summary>
    public int SearchTopK { get; set; } = 5;
}
