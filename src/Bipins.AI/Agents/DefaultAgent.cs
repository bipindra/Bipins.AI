using Bipins.AI.Agents.Memory;
using Bipins.AI.Agents.Planning;
using Bipins.AI.Agents.Tools;
using Bipins.AI.Core.Models;
using Bipins.AI.Providers;
using Microsoft.Extensions.Logging;

namespace Bipins.AI.Agents;

/// <summary>
/// Default implementation of an agent.
/// </summary>
public class DefaultAgent : BaseAgent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultAgent"/> class.
    /// </summary>
    public DefaultAgent(
        string id,
        AgentOptions options,
        ILLMProvider llmProvider,
        IToolRegistry toolRegistry,
        IAgentMemory? memory = null,
        IAgentPlanner? planner = null,
        ILogger<DefaultAgent>? logger = null)
        : base(id, options, llmProvider, toolRegistry, memory, planner, logger)
    {
    }

    /// <inheritdoc />
    public override async Task<AgentResponse> ExecuteAsync(AgentRequest request, CancellationToken cancellationToken = default)
    {
        return await ExecuteAgentLoopAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<AgentResponseChunk> ExecuteStreamAsync(AgentRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // For streaming, we'll execute the loop but stream the LLM responses
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

        conversationHistory.Insert(0, new Message(MessageRole.System, _options.SystemPrompt));

        // Planning phase
        if (_planner != null && _options.EnablePlanning)
        {
            var availableTools = request.AvailableTools ?? _toolRegistry.GetToolDefinitions();
            plan = await _planner.CreatePlanAsync(request, availableTools, memoryContext, cancellationToken);
        }

        var currentGoal = request.Goal;

        for (iterations = 0; iterations < _options.MaxIterations; iterations++)
        {
            var messages = new List<Message>(conversationHistory);
            if (!string.IsNullOrEmpty(currentGoal))
            {
                messages.Add(new Message(MessageRole.User, currentGoal));
            }

            var availableTools = request.AvailableTools ?? _toolRegistry.GetToolDefinitions();
            var chatRequest = new ChatRequest(
                Messages: messages,
                Tools: availableTools.Count > 0 ? availableTools.ToList() : null,
                Temperature: _options.Temperature,
                MaxTokens: _options.MaxTokens);

            // Stream response
            await foreach (var chunk in _llmProvider.ChatStreamAsync(chatRequest, cancellationToken))
            {
                yield return new AgentResponseChunk(
                    Content: chunk.Content,
                    Status: AgentStatus.Executing,
                    IsComplete: chunk.IsComplete);
            }

            // Get final response to check for tool calls
            var finalResponse = await _llmProvider.ChatAsync(chatRequest, cancellationToken);
            conversationHistory.Add(new Message(MessageRole.User, currentGoal));
            conversationHistory.Add(new Message(MessageRole.Assistant, finalResponse.Content));

            if (finalResponse.ToolCalls != null && finalResponse.ToolCalls.Count > 0)
            {
                // Execute tools and continue
                foreach (var toolCall in finalResponse.ToolCalls)
                {
                    var tool = _toolRegistry.GetTool(toolCall.Name);
                    if (tool != null)
                    {
                        var result = await tool.ExecuteAsync(toolCall, cancellationToken);
                        currentGoal = $"Tool result: {System.Text.Json.JsonSerializer.Serialize(result.Result)}\n\nContinue: {request.Goal}";
                    }
                }
                continue;
            }

            // Done
            yield return new AgentResponseChunk(
                Content: string.Empty,
                Status: AgentStatus.Completed,
                IsComplete: true);
            break;
        }
    }
}
