using Bipins.AI.Core.Models;

namespace Bipins.AI.Agents.Tools;

/// <summary>
/// Registry for managing available tools.
/// </summary>
public interface IToolRegistry
{
    /// <summary>
    /// Registers a tool executor.
    /// </summary>
    void RegisterTool(IToolExecutor tool);

    /// <summary>
    /// Gets a tool executor by name.
    /// </summary>
    IToolExecutor? GetTool(string name);

    /// <summary>
    /// Gets all registered tools.
    /// </summary>
    IReadOnlyList<IToolExecutor> GetAllTools();

    /// <summary>
    /// Gets tool definitions for all registered tools (for passing to LLM).
    /// </summary>
    IReadOnlyList<ToolDefinition> GetToolDefinitions();

    /// <summary>
    /// Gets tool definitions for specific tool names.
    /// </summary>
    IReadOnlyList<ToolDefinition> GetToolDefinitions(IReadOnlyList<string> toolNames);
}
