# Bipins.AI Agent Samples

This sample application demonstrates the Agentic AI capabilities of Bipins.AI, including tool execution, memory, planning, streaming, and custom tool creation.

## Overview

The sample includes six comprehensive scenarios that showcase different aspects of the Agentic AI framework:

1. **Basic Agent Execution** - Simple agent with calculator tool
2. **Agent with Multiple Tools** - Agent using multiple tools in sequence
3. **Agent with Memory** - Conversation context and memory across multiple requests
4. **Agent with Planning** - Complex multi-step tasks with execution planning
5. **Streaming Agent Execution** - Real-time streaming of agent responses
6. **Agent with Vector Search** - RAG integration with vector search tool (optional)

## Prerequisites

- .NET 8.0 SDK or later
- OpenAI API key (required for all scenarios)
- Qdrant instance (optional, only for Scenario 6)

## Configuration

### Option 1: User Secrets (Recommended for Development)

```bash
dotnet user-secrets set "OpenAI:ApiKey" "your-api-key-here"
```

### Option 2: Environment Variables

```bash
export OPENAI_API_KEY="your-api-key-here"
```

### Option 3: appsettings.json

Edit `appsettings.json` and add your API key:

```json
{
  "OpenAI": {
    "ApiKey": "your-api-key-here",
    "DefaultChatModelId": "gpt-4o-mini"
  }
}
```

### Optional: Qdrant Configuration (for Scenario 6)

```bash
export QDRANT_ENDPOINT="http://localhost:6333"
```

Or in `appsettings.json`:

```json
{
  "Qdrant": {
    "Endpoint": "http://localhost:6333",
    "CollectionName": "documents",
    "VectorSize": 1536
  }
}
```

## Running the Sample

### Interactive Mode (Default)

Run the sample to get an interactive menu where you can choose which scenarios to run:

```bash
cd samples/Bipins.AI.AgentSamples
dotnet run
```

The interactive menu allows you to:
- Select individual scenarios to run
- Run all scenarios at once
- See detailed execution information for each scenario
- Exit when done

### Run All Scenarios

To run all scenarios automatically without the interactive menu:

```bash
dotnet run -- --all
```

## Features

- **Interactive Menu**: Choose which scenarios to run
- **Detailed Output**: See execution time, iterations, tool calls, and more
- **Progress Indicators**: Real-time feedback during agent execution
- **Error Handling**: Graceful error messages and recovery
- **Agent Information**: Display agent capabilities and configuration
- **Timing Information**: See how long each scenario takes to execute

## Scenarios Explained

### Scenario 1: Basic Agent Execution

Demonstrates the simplest use case - an agent executing a goal that requires a single tool call. The agent uses the built-in calculator tool to perform a mathematical calculation.

**Key Concepts:**
- Agent registration and configuration
- Built-in tool usage (CalculatorTool)
- Basic agent execution flow
- Execution timing and metrics

### Scenario 2: Agent with Multiple Tools

Shows how an agent can use multiple tools in sequence to accomplish a complex goal. The agent uses both the weather tool and calculator tool to get weather information and perform temperature conversion.

**Key Concepts:**
- Multiple tool execution
- Tool result chaining
- Custom tool implementation

### Scenario 3: Agent with Memory

Demonstrates conversation memory across multiple requests in the same session. The agent remembers previous context and can reference earlier interactions.

**Key Concepts:**
- Session-based memory
- Conversation context
- Memory persistence across requests

### Scenario 4: Agent with Planning

Shows how agents create and execute multi-step plans for complex goals. The agent generates an execution plan before starting, then follows it step by step.

**Key Concepts:**
- LLM-based planning
- Execution plan generation
- Plan step execution

### Scenario 5: Streaming Agent Execution

Demonstrates real-time streaming of agent responses as they are generated. This is useful for providing immediate feedback to users.

**Key Concepts:**
- Async enumerable streaming
- Real-time response chunks
- Streaming with tool execution
- Chunk counting and timing

### Scenario 6: Agent with Vector Search (Optional)

Shows integration with RAG capabilities through the vector search tool. The agent can search a knowledge base and use retrieved information to answer questions.

**Key Concepts:**
- Vector search tool
- RAG integration with agents
- Knowledge base querying

## Custom Tool Implementation

The sample includes a custom `WeatherTool` implementation that demonstrates how to create your own tools for agents:

```csharp
public class WeatherTool : IToolExecutor
{
    public string Name => "get_weather";
    public string Description => "Gets the current weather for a given location";
    public JsonElement ParametersSchema => /* JSON schema */;
    
    public Task<ToolExecutionResult> ExecuteAsync(
        ToolCall toolCall, 
        CancellationToken cancellationToken)
    {
        // Implement tool logic
    }
}
```

**Key Points:**
- Implement `IToolExecutor` interface
- Define tool name, description, and parameter schema
- Return `ToolExecutionResult` with success status and result data
- Register tool using `AddTool()` extension method

## Code Walkthrough

### Service Registration

```csharp
services
    .AddBipinsAI()
    .AddOpenAI(o => { /* config */ })
    .AddBipinsAIAgents()
    .AddCalculatorTool()
    .AddTool(new WeatherTool())
    .AddAgent("assistant", options =>
    {
        options.Name = "AI Assistant";
        options.SystemPrompt = "You are a helpful assistant...";
        options.EnablePlanning = true;
        options.EnableMemory = true;
        options.MaxIterations = 10;
    });
```

### Agent Execution

```csharp
var agent = agentRegistry.GetAgent("assistant");
var request = new AgentRequest(
    Goal: "Calculate 15 * 23 + 42",
    SessionId: "session-123");

var response = await agent.ExecuteAsync(request);
```

### Streaming Execution

```csharp
await foreach (var chunk in agent.ExecuteStreamAsync(request))
{
    Console.Write(chunk.Content);
    if (chunk.IsComplete)
    {
        // Handle completion
    }
}
```

## Architecture

The sample demonstrates the following architecture:

```
┌─────────────┐
│   Agent     │
│  (IAgent)   │
└──────┬──────┘
       │
       ├──► Tool Registry ──► Tools (Calculator, Weather, VectorSearch)
       ├──► Memory ──────────► Conversation Context
       ├──► Planner ─────────► Execution Plans
       └──► LLM Provider ────► OpenAI/Azure/Anthropic
```

## Troubleshooting

### Agent Not Found

If you see "Agent 'assistant' not found", check that:
- `AddBipinsAIAgents()` is called
- `AddAgent()` is called with the correct name
- Services are registered in the correct order

### Tool Not Executing

If tools aren't being called:
- Verify tool is registered with `AddTool()` or `AddCalculatorTool()`
- Check tool name matches exactly
- Ensure tool's `ParametersSchema` is valid JSON schema

### Memory Not Working

If memory isn't persisting:
- Verify `EnableMemory = true` in agent options
- Check that `SessionId` is provided in `AgentRequest`
- Ensure `IAgentMemory` is registered (default: `InMemoryAgentMemory`)

## Next Steps

- Explore creating more complex custom tools
- Integrate with your own vector store for RAG
- Build multi-agent systems using the agent registry
- Implement custom planners for domain-specific planning
- Use vector store memory for production deployments

## Related Documentation

- [Bipins.AI README](../../README.md) - Main library documentation
- [Agent API Reference](../../src/Bipins.AI/Agents/) - Agent interfaces and types
- [Tool Implementation Guide](../../src/Bipins.AI/Agents/Tools/) - Tool development guide
