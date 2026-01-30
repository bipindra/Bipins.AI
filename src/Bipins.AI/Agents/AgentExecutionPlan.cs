namespace Bipins.AI.Agents;

/// <summary>
/// Execution plan created by the agent planner.
/// </summary>
/// <param name="Goal">The goal this plan addresses.</param>
/// <param name="Steps">Ordered list of steps to execute.</param>
/// <param name="Reasoning">Optional reasoning for the plan.</param>
/// <param name="Metadata">Additional metadata about the plan.</param>
public record AgentExecutionPlan(
    string Goal,
    IReadOnlyList<PlanStep> Steps,
    string? Reasoning = null,
    Dictionary<string, object>? Metadata = null);

/// <summary>
/// A single step in an execution plan.
/// </summary>
/// <param name="Order">Order of execution (1-based).</param>
/// <param name="Action">Description of the action to take.</param>
/// <param name="ToolName">Optional tool name if this step requires a tool.</param>
/// <param name="Parameters">Optional parameters for the tool or action.</param>
/// <param name="ExpectedOutcome">Optional description of expected outcome.</param>
public record PlanStep(
    int Order,
    string Action,
    string? ToolName = null,
    Dictionary<string, object>? Parameters = null,
    string? ExpectedOutcome = null);
