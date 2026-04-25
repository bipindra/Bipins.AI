using System.Text.Json;
using Bipins.AI.Agents.Memory;
using Bipins.AI.Core.Models;
using Bipins.AI.Providers;
using Bipins.AI.SemanticKernel;
using Microsoft.Extensions.Logging;
using PlanStep = Bipins.AI.Agents.PlanStep;

namespace Bipins.AI.Agents.Planning;

/// <summary>
/// LLM-based planner that uses an LLM to generate execution plans.
/// </summary>
public class LLMPlanner : IAgentPlanner
{
    private readonly ILLMProvider _llmProvider;
    private readonly ISemanticKernelBridge _semanticKernelBridge;
    private readonly ILogger<LLMPlanner>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LLMPlanner"/> class.
    /// </summary>
    public LLMPlanner(
        ILLMProvider llmProvider,
        ISemanticKernelBridge semanticKernelBridge,
        ILogger<LLMPlanner>? logger = null)
    {
        _llmProvider = llmProvider;
        _semanticKernelBridge = semanticKernelBridge;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AgentExecutionPlan> CreatePlanAsync(
        AgentRequest request,
        IReadOnlyList<ToolDefinition> availableTools,
        AgentMemoryContext? context = null,
        CancellationToken cancellationToken = default)
    {
        var planningPrompt = BuildPlanningPrompt(request, availableTools, context);

        var messages = new List<Message>
        {
            new(MessageRole.System, "You are a planning assistant. Create a step-by-step plan to accomplish the given goal. Return the plan as JSON with this structure: {\"steps\": [{\"order\": 1, \"action\": \"...\", \"toolName\": \"...\", \"parameters\": {...}, \"expectedOutcome\": \"...\"}], \"reasoning\": \"...\"}"),
            new(MessageRole.User, planningPrompt)
        };

        var chatRequest = new ChatRequest(
            Messages: messages,
            Temperature: 0.3f, // Lower temperature for more deterministic planning
            MaxTokens: 2000,
            StructuredOutput: new StructuredOutputOptions(
                Schema: JsonSerializer.SerializeToElement(new
                {
                    type = "object",
                    properties = new
                    {
                        steps = new
                        {
                            type = "array",
                            items = new
                            {
                                type = "object",
                                properties = new
                                {
                                    order = new { type = "integer" },
                                    action = new { type = "string" },
                                    toolName = new { type = "string" },
                                    parameters = new { type = "string" },
                                    expectedOutcome = new { type = "string" }
                                },
                                required = new[] { "order", "action", "toolName", "parameters", "expectedOutcome" },
                                additionalProperties = false
                            }
                        },
                        reasoning = new { type = "string" }
                    },
                    required = new[] { "steps", "reasoning" },
                    additionalProperties = false
                }),                
                ResponseFormat: "json_schema"));

        try
        {
            var response = await _llmProvider.ChatAsync(chatRequest, cancellationToken);

            if (response.StructuredOutput.HasValue)
            {
                return ParsePlanFromJson(response.StructuredOutput.Value, request.Goal);
            }

            var parsed = StructuredOutputHelper.ExtractStructuredOutput(response.Content);
            if (parsed.HasValue)
            {
                return ParsePlanFromJson(parsed.Value, request.Goal);
            }

            return ParsePlanFromContent(response.Content, request.Goal);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to create plan using LLM, creating simple plan");
            return CreateSimplePlan(request.Goal);
        }
    }

    private string BuildPlanningPrompt(AgentRequest request, IReadOnlyList<ToolDefinition> availableTools, AgentMemoryContext? context)
    {
        return _semanticKernelBridge.RenderPlanningPrompt(new PlanningPromptTemplate(
            request.Goal,
            request.Context,
            context?.ConversationHistory ?? Array.Empty<Message>(),
            availableTools));
    }

    private AgentExecutionPlan ParsePlanFromJson(JsonElement json, string goal)
    {
        var steps = new List<PlanStep>();

        if (json.TryGetProperty("steps", out var stepsElement) && stepsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var stepElement in stepsElement.EnumerateArray())
            {
                var order = stepElement.TryGetProperty("order", out var orderProp) 
                    ? orderProp.GetInt32() 
                    : steps.Count + 1;
                var action = stepElement.TryGetProperty("action", out var actionProp) 
                    ? actionProp.GetString() ?? "Unknown action" 
                    : "Unknown action";
                var toolName = stepElement.TryGetProperty("toolName", out var toolProp) 
                    ? toolProp.GetString() 
                    : null;
                Dictionary<string, object>? parameters = null;
                if (stepElement.TryGetProperty("parameters", out var paramsProp))
                {
                    if (paramsProp.ValueKind == JsonValueKind.Object)
                        parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(paramsProp.GetRawText());
                    else if (paramsProp.ValueKind == JsonValueKind.String)
                    {
                        var paramsJson = paramsProp.GetString();
                        if (!string.IsNullOrWhiteSpace(paramsJson))
                            parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(paramsJson);
                    }
                }
                var expectedOutcome = stepElement.TryGetProperty("expectedOutcome", out var outcomeProp) 
                    ? outcomeProp.GetString() 
                    : null;

                steps.Add(new PlanStep(order, action, toolName, parameters, expectedOutcome));
            }
        }

        var reasoning = json.TryGetProperty("reasoning", out var reasoningProp) 
            ? reasoningProp.GetString() 
            : null;

        return new AgentExecutionPlan(goal, steps.OrderBy(s => s.Order).ToList(), reasoning);
    }

    private AgentExecutionPlan ParsePlanFromContent(string content, string goal)
    {
        // Simple fallback: create a single-step plan
        return new AgentExecutionPlan(
            goal,
            new List<PlanStep> { new PlanStep(1, content) },
            "Plan generated from LLM response");
    }

    private AgentExecutionPlan CreateSimplePlan(string goal)
    {
        return new AgentExecutionPlan(
            goal,
            new List<PlanStep> { new PlanStep(1, $"Accomplish goal: {goal}") },
            "Simple fallback plan");
    }
}
