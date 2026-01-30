using System.Text.Json;
using Bipins.AI.Agents.Memory;
using Bipins.AI.Agents.Planning;
using Bipins.AI.Agents.Tools;
using Bipins.AI.Core.Models;
using Bipins.AI.Providers;
using Microsoft.Extensions.Logging;

namespace Bipins.AI.Agents;

/// <summary>
/// Base implementation of an agent with common functionality.
/// </summary>
public abstract class BaseAgent : IAgent
{
    protected readonly ILLMProvider _llmProvider;
    protected readonly IToolRegistry _toolRegistry;
    protected readonly IAgentMemory? _memory;
    protected readonly IAgentPlanner? _planner;
    protected readonly AgentOptions _options;
    protected readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseAgent"/> class.
    /// </summary>
    protected BaseAgent(
        string id,
        AgentOptions options,
        ILLMProvider llmProvider,
        IToolRegistry toolRegistry,
        IAgentMemory? memory = null,
        IAgentPlanner? planner = null,
        ILogger? logger = null)
    {
        Id = id;
        Name = options.Name;
        _options = options;
        _llmProvider = llmProvider;
        _toolRegistry = toolRegistry;
        _memory = memory;
        _planner = planner;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

        Capabilities = AgentCapabilities.ToolExecution;
        if (_memory != null) Capabilities |= AgentCapabilities.Memory;
        if (_planner != null && _options.EnablePlanning) Capabilities |= AgentCapabilities.Planning;
        Capabilities |= AgentCapabilities.Streaming;
    }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public AgentCapabilities Capabilities { get; }

    /// <inheritdoc />
    public abstract Task<AgentResponse> ExecuteAsync(AgentRequest request, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract IAsyncEnumerable<AgentResponseChunk> ExecuteStreamAsync(AgentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the core agent loop: plan, execute, use tools, repeat.
    /// </summary>
    protected async Task<AgentResponse> ExecuteAgentLoopAsync(
        AgentRequest request,
        CancellationToken cancellationToken)
    {
        var iterations = 0;
        AgentExecutionPlan? plan = null;
        var conversationHistory = new List<Message>();

        // Load memory context if enabled
        AgentMemoryContext? memoryContext = null;
        if (_memory != null && _options.EnableMemory && !string.IsNullOrEmpty(request.SessionId))
        {
            memoryContext = await _memory.LoadContextAsync(Id, request.SessionId, _options.MemoryOptions?.MaxConversationTurns ?? 50, cancellationToken);
            conversationHistory.AddRange(memoryContext.ConversationHistory);
        }

        // Add system prompt
        conversationHistory.Insert(0, new Message(MessageRole.System, _options.SystemPrompt));

        // Planning phase
        if (_planner != null && _options.EnablePlanning)
        {
            var availableTools = request.AvailableTools ?? _toolRegistry.GetToolDefinitions();
            plan = await _planner.CreatePlanAsync(request, availableTools, memoryContext, cancellationToken);
            _logger.LogDebug("Created plan with {StepCount} steps for agent {AgentId}", plan.Steps.Count, Id);
        }

        // Main execution loop
        var currentGoal = request.Goal;
        var accumulatedContent = new System.Text.StringBuilder();

        for (iterations = 0; iterations < _options.MaxIterations; iterations++)
        {
            // Build messages for LLM
            var messages = new List<Message>(conversationHistory);

            // Add current request
            if (!string.IsNullOrEmpty(currentGoal))
            {
                messages.Add(new Message(MessageRole.User, currentGoal));
            }

            // Add plan context if available
            if (plan != null && iterations == 0)
            {
                var planText = $"Plan: {plan.Reasoning}\nSteps:\n" +
                    string.Join("\n", plan.Steps.Select(s => $"{s.Order}. {s.Action}"));
                messages.Add(new Message(MessageRole.System, planText));
            }

            // Get available tools
            var availableTools = request.AvailableTools ?? _toolRegistry.GetToolDefinitions();
            var toolDefinitions = availableTools.ToList();

            // Generate response
            var chatRequest = new ChatRequest(
                Messages: messages,
                Tools: toolDefinitions.Count > 0 ? toolDefinitions : null,
                Temperature: _options.Temperature,
                MaxTokens: _options.MaxTokens);

            var response = await _llmProvider.ChatAsync(chatRequest, cancellationToken);
            accumulatedContent.Append(response.Content);

            // Add response to conversation history
            conversationHistory.Add(new Message(MessageRole.User, currentGoal));
            conversationHistory.Add(new Message(MessageRole.Assistant, response.Content));

            // Check for tool calls
            if (response.ToolCalls != null && response.ToolCalls.Count > 0)
            {
                _logger.LogDebug("Agent {AgentId} executing {ToolCount} tool calls", Id, response.ToolCalls.Count);

                // Execute tools
                var toolResults = new List<ToolExecutionResult>();
                foreach (var toolCall in response.ToolCalls)
                {
                    var tool = _toolRegistry.GetTool(toolCall.Name);
                    if (tool == null)
                    {
                        toolResults.Add(new ToolExecutionResult(
                            Success: false,
                            Error: $"Tool '{toolCall.Name}' not found"));
                        continue;
                    }

                    try
                    {
                        var result = await tool.ExecuteAsync(toolCall, cancellationToken);
                        toolResults.Add(result);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error executing tool {ToolName}", toolCall.Name);
                        toolResults.Add(new ToolExecutionResult(
                            Success: false,
                            Error: ex.Message));
                    }
                }

                // Format tool results for next iteration
                var toolResultsText = FormatToolResults(toolResults);
                currentGoal = $"Tool execution results:\n{toolResultsText}\n\nContinue working towards the goal: {request.Goal}";
                continue;
            }

            // No tool calls - we're done
            var finalResponse = new AgentResponse(
                Content: accumulatedContent.ToString(),
                Status: AgentStatus.Completed,
                ToolCalls: response.ToolCalls,
                Plan: plan,
                Iterations: iterations + 1);

            // Save to memory
            if (_memory != null && _options.EnableMemory && conversationHistory.Count >= 2)
            {
                var lastUserMessage = conversationHistory[conversationHistory.Count - 2];
                var lastAssistantMessage = conversationHistory[conversationHistory.Count - 1];
                await _memory.SaveAsync(Id, request.SessionId, lastUserMessage, lastAssistantMessage, null, cancellationToken);
            }

            return finalResponse;
        }

        // Max iterations reached
        return new AgentResponse(
            Content: accumulatedContent.ToString(),
            Status: AgentStatus.MaxIterationsReached,
            Plan: plan,
            Iterations: iterations);
    }

    private string FormatToolResults(IReadOnlyList<ToolExecutionResult> results)
    {
        var formatted = new System.Text.StringBuilder();
        for (int i = 0; i < results.Count; i++)
        {
            var result = results[i];
            formatted.AppendLine($"Tool {i + 1}:");
            if (result.Success)
            {
                formatted.AppendLine($"  Result: {JsonSerializer.Serialize(result.Result)}");
            }
            else
            {
                formatted.AppendLine($"  Error: {result.Error}");
            }
        }
        return formatted.ToString();
    }
}
