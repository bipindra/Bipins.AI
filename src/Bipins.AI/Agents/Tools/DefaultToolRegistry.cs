using System.Text.Json;
using Bipins.AI.Core.Models;
using Microsoft.Extensions.Logging;

namespace Bipins.AI.Agents.Tools;

/// <summary>
/// Default implementation of tool registry.
/// </summary>
public class DefaultToolRegistry : IToolRegistry
{
    private readonly Dictionary<string, IToolExecutor> _tools = new();
    private readonly ILogger<DefaultToolRegistry>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultToolRegistry"/> class.
    /// </summary>
    public DefaultToolRegistry(ILogger<DefaultToolRegistry>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public void RegisterTool(IToolExecutor tool)
    {
        if (string.IsNullOrWhiteSpace(tool.Name))
        {
            throw new ArgumentException("Tool name cannot be null or empty.", nameof(tool));
        }

        _tools[tool.Name] = tool;
        _logger?.LogDebug("Registered tool: {ToolName}", tool.Name);
    }

    /// <inheritdoc />
    public IToolExecutor? GetTool(string name)
    {
        _tools.TryGetValue(name, out var tool);
        return tool;
    }

    /// <inheritdoc />
    public IReadOnlyList<IToolExecutor> GetAllTools()
    {
        return _tools.Values.ToList();
    }

    /// <inheritdoc />
    public IReadOnlyList<ToolDefinition> GetToolDefinitions()
    {
        return _tools.Values
            .Select(t => new ToolDefinition(t.Name, t.Description, t.ParametersSchema))
            .ToList();
    }

    /// <inheritdoc />
    public IReadOnlyList<ToolDefinition> GetToolDefinitions(IReadOnlyList<string> toolNames)
    {
        return toolNames
            .Where(name => _tools.ContainsKey(name))
            .Select(name => _tools[name])
            .Select(t => new ToolDefinition(t.Name, t.Description, t.ParametersSchema))
            .ToList();
    }
}
